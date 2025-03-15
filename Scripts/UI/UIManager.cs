using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public enum PanelType
    {
        Settings,
        Inventory,
        SkillTree
    }

    public GameObject settingsPanel;
    public GameObject inventoryPanel;
    public GameObject skillTreePanel;

    // Ссылки на компоненты, которые нужно включать/выключать
    [SerializeField] private SkillTreeNavigation skillTreeNavigation;
    [SerializeField] private SkillTreeManager skillTreeManager;
    [SerializeField] private ThirdPersonCharacter player; // Ссылка на Player.cs
    [SerializeField] private ThirdPersonCamera cameraController; // Ссылка на Camera.cs (предполагается, что у вас есть такой скрипт)

    private GameObject currentPanel;
    private Dictionary<PanelType, GameObject> panelMap;
    private Dictionary<KeyCode, PanelType> keyMap;

    public static UIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        panelMap = new Dictionary<PanelType, GameObject>
        {
            { PanelType.Settings, settingsPanel },
            { PanelType.Inventory, inventoryPanel },
            { PanelType.SkillTree, skillTreePanel }
        };

        foreach (var pair in panelMap)
        {
            if (pair.Value == null)
            {
                Debug.LogError($"Panel {pair.Key} is not assigned in the Inspector!");
                Destroy(gameObject);
                return;
            }
        }

        // Проверка компонентов
        if (skillTreeNavigation == null) Debug.LogError("SkillTreeNavigation не назначен в UIManager!");
        if (skillTreeManager == null) Debug.LogError("SkillTreeManager не назначен в UIManager!");
        if (player == null) Debug.LogError("Player не назначен в UIManager!");
        if (cameraController == null) Debug.LogError("CameraController не назначен в UIManager!");

        keyMap = new Dictionary<KeyCode, PanelType>
        {
            { KeyCode.Escape, PanelType.Settings },
            { KeyCode.Tab, PanelType.Inventory },
            { KeyCode.U, PanelType.SkillTree }
        };

        Cursor.visible = false;
        HideAllPanels();
        UpdateSkillComponentsState();
        UpdatePlayerAndCameraState(); // Инициализируем состояние игрока и камеры
    }

    void Update()
    {
        foreach (var key in keyMap.Keys)
        {
            if (Input.GetKeyDown(key))
            {
                TogglePanel(keyMap[key]);
                break;
            }
        }
    }

    private void TogglePanel(PanelType panelType)
    {
        if (currentPanel != null)
        {
            if (currentPanel == panelMap[panelType] || panelType == PanelType.Settings)
            {
                CloseCurrentPanel();
                return;
            }
            return; // Если другая панель уже открыта, ничего не делаем
        }

        OpenPanel(panelType);
    }

    private void OpenPanel(PanelType panelType)
    {
        HideAllPanels();
        currentPanel = panelMap[panelType];
        currentPanel.SetActive(true);
        SetGamePaused(panelType != PanelType.Inventory);
        UpdateSkillComponentsState();
        UpdatePlayerAndCameraState(); // Обновляем состояние игрока и камеры
    }

    private void CloseCurrentPanel()
    {
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
            SetGamePaused(false);
            UpdateSkillComponentsState();
            UpdatePlayerAndCameraState(); // Обновляем состояние игрока и камеры
        }
    }

    private void HideAllPanels()
    {
        foreach (var panel in panelMap.Values)
        {
            panel.SetActive(false);
        }
        currentPanel = null;
        UpdateSkillComponentsState();
        UpdatePlayerAndCameraState(); // Обновляем состояние игрока и камеры
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

    // Метод для управления состоянием компонентов дерева навыков
    private void UpdateSkillComponentsState()
    {
        bool isSkillTreeActive = skillTreePanel.activeInHierarchy;

        if (skillTreeNavigation != null)
        {
            skillTreeNavigation.enabled = isSkillTreeActive;
        }

        if (skillTreeManager != null)
        {
            skillTreeManager.enabled = isSkillTreeActive;
        }
    }

    // Метод для управления состоянием игрока и камеры
    private void UpdatePlayerAndCameraState()
    {
        bool isSettingsOrSkillTreeActive = settingsPanel.activeInHierarchy || skillTreePanel.activeInHierarchy;

        if (player != null)
        {
            player.enabled = !isSettingsOrSkillTreeActive; // Отключаем Player.cs, если открыты Settings или SkillTree
        }

        if (cameraController != null)
        {
            cameraController.enabled = !isSettingsOrSkillTreeActive; // Отключаем Camera.cs, если открыты Settings или SkillTree
        }
    }
}