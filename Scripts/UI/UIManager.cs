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

        keyMap = new Dictionary<KeyCode, PanelType>
        {
            { KeyCode.Escape, PanelType.Settings },
            { KeyCode.Tab, PanelType.Inventory },
            { KeyCode.U, PanelType.SkillTree }
        };

        Cursor.visible = false;
        HideAllPanels();
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
    }

    private void CloseCurrentPanel()
    {
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
            SetGamePaused(false);
        }
    }

    private void HideAllPanels()
    {
        foreach (var panel in panelMap.Values)
        {
            panel.SetActive(false);
        }
        currentPanel = null;
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
}