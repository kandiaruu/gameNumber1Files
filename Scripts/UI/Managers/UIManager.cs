using UnityEngine;
using System.Collections.Generic;
public class UIManager : MonoBehaviour, IUIManager
{
    public enum PanelType
    {
        Settings,
        Inventory,
        SkillTree,
        Notification,
        Tooltip
    }

    [System.Serializable]
    public class PanelConfig
    {
        public PanelType panelType;
        public GameObject panelObject;
        public MonoBehaviour[] scriptsToDisable;
        public List<PanelConfig> childPanels = new List<PanelConfig>();
    }

    [System.Serializable]
    public class PanelScriptControl
    {
        public PanelType panelType;
        [Tooltip("Скрипты, которые будут отключены при активации этой панели")]
        public MonoBehaviour[] scriptsToDisable;
    }

    [SerializeField] private List<PanelConfig> panelConfigs = new List<PanelConfig>();
    [InjectAttribute1]
    private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1]
    private ISkillTreeManager skillLogicManager{ get; set; }
    [InjectAttribute1]
    private ISkillUIManager skillUIManager{ get; set; }
    [InjectAttribute1]
    private INotificationManager notificationManager{ get; set; }
    [SerializeField] private List<PanelScriptControl> panelScriptControls = new List<PanelScriptControl>();
    private Dictionary<PanelType, GameObject> panelCache = new Dictionary<PanelType, GameObject>();

    private IPanel currentPanel;
    private Dictionary<KeyCode, PanelType> keyMap;

    void Awake()
    {
        // Проверяем на дубликаты

        //DependencyContainer.Instance.RegisterManual(this);

        InitializePanelSystem();

        keyMap = new Dictionary<KeyCode, PanelType>
        {
            { KeyCode.Escape, PanelType.Settings },
            { KeyCode.Tab, PanelType.Inventory },
            { KeyCode.U, PanelType.SkillTree }
        };

        Cursor.visible = false;
        CachePanels();
    }
    void Start()
    {
        HideAllPanels(); // Moved to Start to ensure all Awake() methods have completed
    }

    // Остальной код UIManager без изменений...
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
                    Debug.Log($"Кэширована панель: {config.panelType} -> {config.panelObject.name}");
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
                CloseCurrentPanel();
            }
            else
            {
                TogglePanel(PanelType.Settings);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel(PanelType.Inventory);
        }
        else if (Input.GetKeyDown(KeyCode.U))
        {
            TogglePanel(PanelType.SkillTree);
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

    private void OpenPanel(PanelType panelType)
    {
        HideAllPanels();
        var config = FindPanelConfigByType(panelConfigs, panelType);
        if (config != null && config.panelObject != null)
        {
            currentPanel = config.panelObject.GetComponent<IPanel>();
            if (currentPanel != null)
            {
                currentPanel.Open();
                SetGamePaused(panelType != PanelType.Inventory);
                UpdateSkillComponentsState();
                UpdateScriptStates(panelType);
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

    private void CloseCurrentPanel()
    {
        if (currentPanel != null)
        {
            currentPanel.Close();
            currentPanel = null;
            SetGamePaused(false);
            UpdateSkillComponentsState();
            UpdateScriptStates(null);
        }
    }

    private void HideAllPanels()
    {
        foreach (var config in panelConfigs)
        {
            HidePanelRecursive(config);
        }
        currentPanel = null;
        UpdateSkillComponentsState();
        UpdateScriptStates(null);
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
        Cursor.visible = paused;
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

        // if (skillTreeNavigation != null)
        // {
        //     skillTreeNavigation.enabled = isSkillTreeActive;
        // }

        // if (skillUIManager != null)
        // {
        //     skillUIManager.enabled = isSkillTreeActive;
        // }
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

    private void InitializePanelRecursive(PanelConfig config)
    {
        if (config.panelObject == null)
        {
            Debug.LogError($"Панель {config.panelType} не назначена в инспекторе!");
            return;
        }
        if (!config.panelObject.GetComponent<BasePanel>())
        {
            config.panelObject.AddComponent<BasePanel>();
        }

        var panel = config.panelObject.GetComponent<IPanel>();
        foreach (var childConfig in config.childPanels)
        {
            InitializePanelRecursive(childConfig);
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
}