using UnityEngine;
using System.Collections.Generic;
using TMPro;
public class UIManager : MonoBehaviour, IUIManager
{
    public enum PanelType
    {
        Settings,
        Inventory,
        SkillTree,
        Notification,
        Tooltip,
        Selection,
        SkillGui,
        ItemInfo,
        Status,
        Inventory2, // Добавлен новый тип панели
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

    [System.Serializable]
    public class PanelConfig
    {
        public PanelType panelType;
        public GameObject panelObject;
        public MonoBehaviour[] scriptsToDisable;
        public List<PanelConfig> childPanels = new List<PanelConfig>();
        public bool showTooltip = false; // По умолчанию тултип включён
        public bool allowNavigation = false; // Новое поле для управления зумом и перетаскиванием
        public bool allowSkillButtonInteraction = false; // Новое поле для управления кнопками
    }

    [System.Serializable]
    public class PanelScriptControl
    {
        public PanelType panelType;
        [Tooltip("Скрипты, которые будут отключены при активации этой панели")]
        public MonoBehaviour[] scriptsToDisable;
    }

    [SerializeField] private GameObject eKeyIcon;
    [SerializeField] private GameObject dotObject;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private GameObject minimapObject;
    [SerializeField] private List<PanelConfig> panelConfigs = new List<PanelConfig>();
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1] private ISkillUIManager skillUIManager{ get; set; }
    [InjectAttribute1] private IInventorySearch3 inventorySearch3 { get; set; }
    [SerializeField] private List<PanelScriptControl> panelScriptControls = new List<PanelScriptControl>();
    private Dictionary<PanelType, GameObject> panelCache = new Dictionary<PanelType, GameObject>();
    private const int MAX_DEPTH = 5;

    private IPanel currentPanel;
    private Dictionary<KeyCode, PanelType> keyMap;
    public Dictionary<PanelType, KeyCode> PanelKeys { get; private set; } = new Dictionary<PanelType, KeyCode>();

    void Awake()
    {

        InitializePanelSystem();

        keyMap = new Dictionary<KeyCode, PanelType>
        {
            { KeyCode.Escape, PanelType.Settings },
            { KeyCode.Tab, PanelType.Inventory },
            { KeyCode.U, PanelType.SkillTree },
            { KeyCode.BackQuote, PanelType.Status },
            { KeyCode.M, PanelType.Map}
        };

        Cursor.visible = false;
        CachePanels();
        LoadPanelKeys();
    }
    void Start()
    {
        HideAllPanels();
    }

    public void LoadPanelKeys()
    {
        PanelKeys[PanelType.Inventory3] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Inventory3", KeyCode.Tab.ToString()));
        PanelKeys[PanelType.SkillTree] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_SkillTree", KeyCode.U.ToString()));
        PanelKeys[PanelType.Status] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Status", KeyCode.BackQuote.ToString()));
        PanelKeys[PanelType.Map] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Map", KeyCode.M.ToString()));
        PanelKeys[PanelType.SkillSelect] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_SkillSelect", KeyCode.O.ToString()));
    }

    public void SetPanelKey(PanelType type, KeyCode key)
    {
        PanelKeys[type] = key;
        PlayerPrefs.SetString("Key_" + type.ToString(), key.ToString());
        PlayerPrefs.Save();
    }
    
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

    private bool IsPanelOrChildOpen(PanelConfig config)
    {
        if (config.panelObject != null && config.panelObject.activeSelf)
        {
            return true;
        }

        return false;
    }

    private void CachePanels()
    {
        panelCache.Clear();
        CachePanelsRecursive(panelConfigs);
    }

    private void CachePanelsRecursive(List<PanelConfig> configs)
    {
        foreach (var config in configs)
        {
            if (config.panelObject != null)
            {
                if (panelCache.ContainsKey(config.panelType))
                {
                    Debug.LogWarning($"Обнаружен дубликат типа панели {config.panelType}. Используется первый найденный объект.");
                }
                else
                {
                    panelCache[config.panelType] = config.panelObject;
                }
            }
            else
            {
                Debug.LogError($"PanelObject для типа {config.panelType} не назначен!");
            }
            CachePanelsRecursive(config.childPanels);
        }
    }

    public GameObject GetPanel(PanelType panelType)
    {
        if (panelCache.TryGetValue(panelType, out GameObject panel))
        {
            return panel;
        }
        Debug.LogError($"Панель типа {panelType} не найдена в кэше!");
        return null;
    }

    public List<PanelConfig> GetPanelConfigs()
    {
        return panelConfigs;
    }

    void Update()
    {
        // Кнопку Escape оставляем жестко зашитой, чтобы игрок случайно не удалил её и не застрял в меню
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
                    Debug.Log("Escape не сработал, так как активна панель инвентаря");
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
                Debug.LogError($"Панель {panelType} не имеет компонента IPanel!");
            }
        }
        else
        {
            Debug.LogError($"Конфигурация для панели {panelType} не найдена!");
        }
    }

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

    public void ActivatePanel(PanelType panelType)
    {
        if (currentPanel != null) return;
        OpenPanel(panelType);
    }

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

    private void InitializePanelSystem()
    {
        foreach (var config in panelConfigs)
        {
            InitializePanelRecursive(config);
        }
    }

    private void InitializePanelRecursive(PanelConfig config, int currentDepth = 0, HashSet<PanelConfig> visited = null)
    {
        if (currentDepth >= MAX_DEPTH)
        {
            Debug.LogError($"Превышена максимальная глубина иерархии ({MAX_DEPTH}) для панели {config.panelType}!");
            return;
        }

        if (config == null || config.panelObject == null)
        {
            Debug.LogError($"Панель {config?.panelType} не назначена или некорректна!");
            return;
        }

        visited = visited ?? new HashSet<PanelConfig>();
        if (!visited.Add(config))
        {
            Debug.LogError($"Обнаружен цикл в конфигурации панели {config.panelType}!");
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

    public bool ShouldAllowSkillButtonInteraction()
    {
        var activePanelConfig = FindActivePanelConfig();
        if (activePanelConfig != null)
        {
            if (!activePanelConfig.allowSkillButtonInteraction)
            {
                Debug.Log($"Взаимодействие с кнопками навыков отключено для панели {activePanelConfig.panelType}");
                return false;
            }
        }
        else
        {
            Debug.Log("Активная панель не найдена, разрешаем взаимодействие с кнопками по умолчанию");
        }
        return true;
    }

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

    public PanelConfig FindActivePanelConfig()
    {
        PanelConfig deepestActiveConfig = null;
        FindActivePanelConfigRecursive(panelConfigs, ref deepestActiveConfig, 0);
        return deepestActiveConfig;
    }
    public bool ShouldAllowNavigation()
        {
            var activePanelConfig = FindActivePanelConfig();
            if (activePanelConfig != null)
            {
                if (!activePanelConfig.allowNavigation)
                {
                    Debug.Log($"Навигация отключена для панели {activePanelConfig.panelType}");
                    return false;
                }
            }
            return true;
        }

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

    private int GetDepth(List<PanelConfig> configs, PanelConfig targetConfig)
    {
        return GetDepthRecursive(configs, targetConfig, 0);
    }

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
                        // На случай, если на панели нет IPanel
                        childConfig.panelObject.SetActive(false); 
                    }
                }
            }
        }
    }
}