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
        Tooltip,
        Selection
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

    [SerializeField] private List<PanelConfig> panelConfigs = new List<PanelConfig>();
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1] private ISkillTreeManager skillLogicManager{ get; set; }
    [InjectAttribute1] private ISkillUIManager skillUIManager{ get; set; }
    [InjectAttribute1] private INotificationManager notificationManager{ get; set; }
    [SerializeField] private List<PanelScriptControl> panelScriptControls = new List<PanelScriptControl>();
    private Dictionary<PanelType, GameObject> panelCache = new Dictionary<PanelType, GameObject>();
    private const int MAX_DEPTH = 5; // Максимальная глубина иерархии

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
            // Проверяем, является ли закрываемая панель SkillTree
            var currentConfig = FindPanelConfig(panelConfigs, currentPanel.PanelObject);
            if (currentConfig != null && currentConfig.panelType == PanelType.SkillTree)
            {
                skillTreeNavigation?.ResetNavigation(); // Сбрасываем позицию и масштаб skillHolder
            }
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
        if (skillTreeNavigation != null)
        {
            skillTreeNavigation.ResetNavigation(); // Сбрасываем при скрытии всех панелей
        }
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

        if (paused)
        {
            Cursor.lockState = CursorLockMode.None; // Разблокируем курсор
            Cursor.visible = true;                  // Показываем курсор
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // Замораживаем курсор по центру
            Cursor.visible = false;                   // Прячем курсор
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
        return true; // Если нет активной панели или взаимодействие разрешено
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
            return true; // Если нет активной панели или навигация разрешена
        }

    private void FindActivePanelConfigRecursive(List<PanelConfig> configs, ref PanelConfig deepestActiveConfig, int currentDepth)
    {
        foreach (var config in configs)
        {
            // Проверяем, активна ли текущая панель
            if (config.panelObject != null && config.panelObject.activeSelf)
            {
                // Если это самая глубокая активная панель на данный момент, обновляем
                if (deepestActiveConfig == null || currentDepth > GetDepth(panelConfigs, deepestActiveConfig))
                {
                    deepestActiveConfig = config;
                }
            }

            // Рекурсивно проверяем дочерние панели
            FindActivePanelConfigRecursive(config.childPanels, ref deepestActiveConfig, currentDepth + 1);
        }
    }

    // Вспомогательный метод для определения глубины панели в иерархии
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
        return -1; // Не нашли
    }
}