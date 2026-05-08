using UnityEngine;
using System.Collections.Generic;
using TMPro;

//
// Central UI manager that owns all panel configurations, handles keyboard-driven panel toggling,
// controls game pause state, manages cursor visibility, and arbitrates navigation/interaction permissions.
//

public class UIManager : MonoBehaviour, IUIManager
{
    // All panel types available in the game
    public enum PanelType
    {
        Settings,
        SkillTree,
        Notification,
        Tooltip,
        Selection,
        SkillGui,
        ItemInfo,
        Status,
        Inventory3,
        Inventory3LootPanel,
        Inventory3LKMPanel,
        Map,
        Loading,
        DungeonEntry,
        MerchantPanel,
        SettingsGeneral,
        SettingsControls,
        SkillSelect
    }

    // Inspector-configured entry linking a panel type to its GameObject, optional scripts to disable, child panels, and feature flags
    [System.Serializable]
    public class PanelConfig
    {
        public PanelType panelType;
        public GameObject panelObject;
        public MonoBehaviour[] scriptsToDisable;
        public List<PanelConfig> childPanels = new List<PanelConfig>();
        public bool showTooltip = false;
        public bool allowNavigation = false;
        public bool allowSkillButtonInteraction = false;
    }

    // Maps a panel type to the scripts that should be disabled when that panel is active
    [System.Serializable]
    public class PanelScriptControl
    {
        public PanelType panelType;
        [Tooltip("Scripts that will be disabled when this panel is activated")]
        public MonoBehaviour[] scriptsToDisable;
    }

    [SerializeField] private GameObject eKeyIcon;
    [SerializeField] private GameObject dotObject;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private GameObject minimapObject;
    [SerializeField] private List<PanelConfig> panelConfigs = new List<PanelConfig>();
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1] private ISkillUIManager skillUIManager { get; set; }
    [InjectAttribute1] private IInventorySearch3 inventorySearch3 { get; set; }
    [SerializeField] private List<PanelScriptControl> panelScriptControls = new List<PanelScriptControl>();

    private Dictionary<PanelType, GameObject> panelCache = new Dictionary<PanelType, GameObject>();
    private const int MAX_DEPTH = 5;

    private IPanel currentPanel;
    private Dictionary<KeyCode, PanelType> keyMap;
    public Dictionary<PanelType, KeyCode> PanelKeys { get; private set; } = new Dictionary<PanelType, KeyCode>();

    // Initializes the panel hierarchy, builds the default key map, hides the cursor, caches panels, and loads saved key bindings
    void Awake()
    {
        InitializePanelSystem();

        keyMap = new Dictionary<KeyCode, PanelType>
        {
            { KeyCode.Escape, PanelType.Settings },
            { KeyCode.Tab, PanelType.Inventory3 },
            { KeyCode.U, PanelType.SkillTree },
            { KeyCode.BackQuote, PanelType.Status },
            { KeyCode.M, PanelType.Map }
        };

        Cursor.visible = false;
        CachePanels();
        LoadPanelKeys();
    }

    // Hides all panels at game start
    void Start()
    {
        HideAllPanels();
    }

    // Loads panel key bindings from PlayerPrefs, falling back to defaults if not set
    public void LoadPanelKeys()
    {
        PanelKeys[PanelType.Inventory3] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Inventory3", KeyCode.Tab.ToString()));
        PanelKeys[PanelType.SkillTree] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_SkillTree", KeyCode.U.ToString()));
        PanelKeys[PanelType.Status] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Status", KeyCode.BackQuote.ToString()));
        PanelKeys[PanelType.Map] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Map", KeyCode.M.ToString()));
        PanelKeys[PanelType.SkillSelect] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_SkillSelect", KeyCode.O.ToString()));
    }

    // Persists a new key binding for the given panel type to PlayerPrefs
    public void SetPanelKey(PanelType type, KeyCode key)
    {
        PanelKeys[type] = key;
        PlayerPrefs.SetString("Key_" + type.ToString(), key.ToString());
        PlayerPrefs.Save();
    }

    // Shows or hides HUD elements (HP/MP text, dot, E-key icon, minimap) based on whether any panel is open
    private void UpdateDotVisibility()
    {
        bool anyPanelOpen = false;
        foreach (var config in panelConfigs)
        {
            if (IsPanelOrChildOpen(config))
            {
                anyPanelOpen = true;
                break;
            }
        }

        if (dotObject != null && eKeyIcon != null && hpText != null && mpText != null && minimapObject != null)
        {
            hpText.gameObject.SetActive(!anyPanelOpen);
            mpText.gameObject.SetActive(!anyPanelOpen);
            eKeyIcon.SetActive(!anyPanelOpen);
            dotObject.SetActive(!anyPanelOpen);
            minimapObject.SetActive(!anyPanelOpen);
        }
    }

    // Returns true if the panel's GameObject is currently active in the scene
    private bool IsPanelOrChildOpen(PanelConfig config)
    {
        if (config.panelObject != null && config.panelObject.activeSelf)
        {
            return true;
        }

        return false;
    }

    // Clears and rebuilds the panel lookup cache from the configured panel list
    private void CachePanels()
    {
        panelCache.Clear();
        CachePanelsRecursive(panelConfigs);
    }

    // Recursively walks the panel config tree and stores each panel's GameObject in the cache by type
    private void CachePanelsRecursive(List<PanelConfig> configs)
    {
        foreach (var config in configs)
        {
            if (config.panelObject != null)
            {
                if (panelCache.ContainsKey(config.panelType))
                {
                    Debug.LogWarning($"Duplicate panel type {config.panelType} detected. Using the first found object.");
                }
                else
                {
                    panelCache[config.panelType] = config.panelObject;
                }
            }
            else
            {
                Debug.LogError($"PanelObject for type {config.panelType} is not assigned!");
            }
            CachePanelsRecursive(config.childPanels);
        }
    }

    // Returns the cached GameObject for the given panel type, or null if not found
    public GameObject GetPanel(PanelType panelType)
    {
        if (panelCache.TryGetValue(panelType, out GameObject panel))
        {
            return panel;
        }
        Debug.LogError($"Panel of type {panelType} not found in cache!");
        return null;
    }

    // Returns the full list of panel configurations
    public List<PanelConfig> GetPanelConfigs()
    {
        return panelConfigs;
    }

    // Polls keyboard input each frame: Escape is hardcoded to close or open settings; other panels use remappable keys
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentPanel != null)
            {
                var currentConfig = FindPanelConfig(panelConfigs, currentPanel.PanelObject);
                if (currentConfig != null)
                {
                    var activeChild = FindActiveChild(currentConfig);
                    if (activeChild != null)
                    {
                        activeChild.Close();
                        return;
                    }
                }
                if (inventorySearch3 != null && inventorySearch3.returnSearching() == false)
                {
                    CloseCurrentPanel();
                }
                else if (inventorySearch3 != null && inventorySearch3.returnSearching() == true)
                {
                    Debug.Log("Escape did not fire because the inventory search panel is active");
                }
                else
                {
                    CloseCurrentPanel();
                }
            }
            else
            {
                TogglePanel(PanelType.Settings);
            }
        }
        else if (PanelKeys.ContainsKey(PanelType.Inventory3) && Input.GetKeyDown(PanelKeys[PanelType.Inventory3]))
        {
            TogglePanel(PanelType.Inventory3);
        }
        else if (PanelKeys.ContainsKey(PanelType.SkillTree) && Input.GetKeyDown(PanelKeys[PanelType.SkillTree]))
        {
            TogglePanel(PanelType.SkillTree);
        }
        else if (PanelKeys.ContainsKey(PanelType.Status) && Input.GetKeyDown(PanelKeys[PanelType.Status]))
        {
            TogglePanel(PanelType.Status);
        }
        else if (PanelKeys.ContainsKey(PanelType.Map) && Input.GetKeyDown(PanelKeys[PanelType.Map]))
        {
            TogglePanel(PanelType.Map);
        }
        else if (PanelKeys.ContainsKey(PanelType.SkillSelect) && Input.GetKeyDown(PanelKeys[PanelType.SkillSelect]))
        {
            TogglePanel(PanelType.SkillSelect);
        }
    }

    // Closes the current panel if it matches the requested type (or if Settings is requested), otherwise opens the panel
    private void TogglePanel(PanelType panelType)
    {
        if (currentPanel != null)
        {
            var currentConfig = FindPanelConfig(panelConfigs, currentPanel.PanelObject);
            if (currentConfig != null && (currentConfig.panelType == panelType || panelType == PanelType.Settings))
            {
                CloseCurrentPanel();
                return;
            }
            return;
        }
        OpenPanel(panelType);
    }

    // Hides all panels, then opens the requested panel, pauses the game, and updates component/script states
    public void OpenPanel(PanelType panelType)
    {
        HideAllPanels();
        var config = FindPanelConfigByType(panelConfigs, panelType);
        if (config != null && config.panelObject != null)
        {
            currentPanel = config.panelObject.GetComponent<IPanel>();
            if (currentPanel != null)
            {
                currentPanel.Open();
                SetGamePaused(true);
                UpdateSkillComponentsState();
                UpdateScriptStates(panelType);
                UpdateDotVisibility();
            }
            else
            {
                Debug.LogError($"Panel {panelType} does not have an IPanel component!");
            }
        }
        else
        {
            Debug.LogError($"Configuration for panel {panelType} not found!");
        }
    }

    // Closes the active panel, unpauses the game, resets skill tree navigation if needed, and clears component/script states
    public void CloseCurrentPanel()
    {
        if (currentPanel != null)
        {
            currentPanel.Close();

            var currentConfig = FindPanelConfig(panelConfigs, currentPanel.PanelObject);
            if (currentConfig != null && currentConfig.panelType == PanelType.SkillTree)
            {
                skillTreeNavigation?.ResetNavigation();
            }
            currentPanel = null;
            SetGamePaused(false);
            UpdateSkillComponentsState();
            UpdateScriptStates(null);
            UpdateDotVisibility();
        }
    }

    // Closes every panel in the hierarchy and resets all associated state
    private void HideAllPanels()
    {
        foreach (var config in panelConfigs)
        {
            HidePanelRecursive(config);
        }
        currentPanel = null;
        if (skillTreeNavigation != null)
        {
            skillTreeNavigation.ResetNavigation();
        }
        UpdateSkillComponentsState();
        UpdateScriptStates(null);
        UpdateDotVisibility();
    }

    // Closes a panel and all of its configured child panels recursively
    private void HidePanelRecursive(PanelConfig config)
    {
        if (config.panelObject != null)
        {
            var panel = config.panelObject.GetComponent<IPanel>();
            if (panel != null)
            {
                panel.Close();
            }
        }
        foreach (var child in config.childPanels)
        {
            HidePanelRecursive(child);
        }
    }

    // Pauses or unpauses the game and shows or locks the cursor accordingly
    private void SetGamePaused(bool paused)
    {
        Time.timeScale = paused ? 0 : 1;

        if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Opens a panel only if no other panel is currently open
    public void ActivatePanel(PanelType panelType)
    {
        if (currentPanel != null) return;
        OpenPanel(panelType);
    }

    // Enables or disables skill tree navigation and skill UI components based on whether the SkillTree panel is active
    private void UpdateSkillComponentsState()
    {
        var skillTreeConfig = FindPanelConfigByType(panelConfigs, PanelType.SkillTree);
        bool isSkillTreeActive = skillTreeConfig != null && skillTreeConfig.panelObject != null && skillTreeConfig.panelObject.activeInHierarchy;

        if (skillTreeNavigation != null)
        {
            if (skillTreeNavigation is MonoBehaviour skillTreeBehaviour)
            {
                skillTreeBehaviour.enabled = isSkillTreeActive;
            }
            else
            {
                Debug.LogWarning("skillTreeNavigation does not inherit from MonoBehaviour and cannot be enabled/disabled.");
            }
        }

        if (skillUIManager != null)
        {
            if (skillUIManager is MonoBehaviour skillUIBehaviour)
            {
                skillUIBehaviour.enabled = isSkillTreeActive;
            }
            else
            {
                Debug.LogWarning("skillUIManager does not inherit from MonoBehaviour and cannot be enabled/disabled.");
            }
        }
    }

    // Re-enables all controlled scripts, then disables those associated with the currently active panel type
    private void UpdateScriptStates(PanelType? activePanelType)
    {
        foreach (var control in panelScriptControls)
        {
            foreach (var script in control.scriptsToDisable)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
        }

        if (activePanelType.HasValue)
        {
            var control = panelScriptControls.Find(c => c.panelType == activePanelType.Value);
            if (control != null)
            {
                foreach (var script in control.scriptsToDisable)
                {
                    if (script != null)
                    {
                        script.enabled = false;
                    }
                }
            }
        }
    }

    // Bootstraps the panel hierarchy by recursively adding BasePanel components and registering child panels
    private void InitializePanelSystem()
    {
        foreach (var config in panelConfigs)
        {
            InitializePanelRecursive(config);
        }
    }

    // Recursively initializes a single panel config entry: guards against depth overflow and cycles, adds BasePanel, and links children
    private void InitializePanelRecursive(PanelConfig config, int currentDepth = 0, HashSet<PanelConfig> visited = null)
    {
        if (currentDepth >= MAX_DEPTH)
        {
            Debug.LogError($"Maximum panel hierarchy depth ({MAX_DEPTH}) exceeded for panel {config.panelType}!");
            return;
        }

        if (config == null || config.panelObject == null)
        {
            Debug.LogError($"Panel {config?.panelType} is not assigned or is invalid!");
            return;
        }

        visited = visited ?? new HashSet<PanelConfig>();
        if (!visited.Add(config))
        {
            Debug.LogError($"Cycle detected in panel configuration for {config.panelType}!");
            return;
        }

        if (!config.panelObject.GetComponent<BasePanel>())
        {
            config.panelObject.AddComponent<BasePanel>();
        }

        var panel = config.panelObject.GetComponent<IPanel>();
        foreach (var childConfig in config.childPanels)
        {
            InitializePanelRecursive(childConfig, currentDepth + 1, visited);
            if (childConfig.panelObject != null)
            {
                var childPanel = childConfig.panelObject.GetComponent<IPanel>();
                if (childPanel != null)
                {
                    panel.AddChild(childPanel);
                }
            }
        }
    }

    // Returns true if the currently active panel allows skill button interaction; true by default when no panel is open
    public bool ShouldAllowSkillButtonInteraction()
    {
        var activePanelConfig = FindActivePanelConfig();
        if (activePanelConfig != null)
        {
            if (!activePanelConfig.allowSkillButtonInteraction)
            {
                Debug.Log($"Skill button interaction disabled for panel {activePanelConfig.panelType}");
                return false;
            }
        }
        else
        {
            Debug.Log("No active panel found, allowing skill button interaction by default");
        }
        return true;
    }

    // Searches the config tree for the entry whose panelObject matches the given GameObject
    private PanelConfig FindPanelConfig(List<PanelConfig> configs, GameObject panelObject)
    {
        foreach (var config in configs)
        {
            if (config.panelObject == panelObject)
            {
                return config;
            }
            var found = FindPanelConfig(config.childPanels, panelObject);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    // Searches the config tree for the entry with the given PanelType
    private PanelConfig FindPanelConfigByType(List<PanelConfig> configs, PanelType panelType)
    {
        foreach (var config in configs)
        {
            if (config.panelType == panelType)
            {
                return config;
            }
            var found = FindPanelConfigByType(config.childPanels, panelType);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    // Returns the first open child panel found under the given config, searching depth-first
    private IPanel FindActiveChild(PanelConfig config)
    {
        foreach (var childConfig in config.childPanels)
        {
            var childPanel = childConfig.panelObject.GetComponent<IPanel>();
            if (childPanel != null && childPanel.IsOpen)
            {
                return childPanel;
            }
            var deeperChild = FindActiveChild(childConfig);
            if (deeperChild != null)
            {
                return deeperChild;
            }
        }
        return null;
    }

    // Returns the deepest active panel configuration in the hierarchy
    public PanelConfig FindActivePanelConfig()
    {
        PanelConfig deepestActiveConfig = null;
        FindActivePanelConfigRecursive(panelConfigs, ref deepestActiveConfig, 0);
        return deepestActiveConfig;
    }

    // Returns true if the currently active panel allows skill tree navigation; true when no panel is open
    public bool ShouldAllowNavigation()
    {
        var activePanelConfig = FindActivePanelConfig();
        if (activePanelConfig != null)
        {
            if (!activePanelConfig.allowNavigation)
            {
                Debug.Log($"Navigation disabled for panel {activePanelConfig.panelType}");
                return false;
            }
        }
        return true;
    }

    // Recursively walks all configs and tracks the deepest active one
    private void FindActivePanelConfigRecursive(List<PanelConfig> configs, ref PanelConfig deepestActiveConfig, int currentDepth)
    {
        foreach (var config in configs)
        {
            if (config.panelObject != null && config.panelObject.activeSelf)
            {
                if (deepestActiveConfig == null || currentDepth > GetDepth(panelConfigs, deepestActiveConfig))
                {
                    deepestActiveConfig = config;
                }
            }
            FindActivePanelConfigRecursive(config.childPanels, ref deepestActiveConfig, currentDepth + 1);
        }
    }

    // Returns the nesting depth of the target config within the config tree
    private int GetDepth(List<PanelConfig> configs, PanelConfig targetConfig)
    {
        return GetDepthRecursive(configs, targetConfig, 0);
    }

    // Recursively searches for targetConfig and returns its depth, or -1 if not found
    private int GetDepthRecursive(List<PanelConfig> configs, PanelConfig targetConfig, int currentDepth)
    {
        foreach (var config in configs)
        {
            if (config == targetConfig)
            {
                return currentDepth;
            }
            int childDepth = GetDepthRecursive(config.childPanels, targetConfig, currentDepth + 1);
            if (childDepth != -1)
            {
                return childDepth;
            }
        }
        return -1;
    }

    // Closes all immediate child panels of the given parent panel type
    public void CloseAllChildren(PanelType parentPanelType)
    {
        var parentConfig = FindPanelConfigByType(panelConfigs, parentPanelType);
        if (parentConfig != null)
        {
            foreach (var childConfig in parentConfig.childPanels)
            {
                if (childConfig.panelObject != null)
                {
                    var childPanel = childConfig.panelObject.GetComponent<IPanel>();
                    if (childPanel != null)
                    {
                        childPanel.Close();
                    }
                    else
                    {
                        childConfig.panelObject.SetActive(false);
                    }
                }
            }
        }
    }
}
