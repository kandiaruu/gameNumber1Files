using UnityEngine;
using System.Collections.Generic;

public class SkillPanelManager : MonoBehaviour, ISkillPanelManager
{
    public enum SkillPanelState
    {
        Normal,
        Hidden
    }

    [InjectAttribute1] private ISkillPanelUI skillPanelUI { get; set; }
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1] private IUIManager uiManager { get; set; } // Добавляем зависимость на UIManager
    [SerializeField] private List<PanelData> panels;

    private SkillPanelState currentSkillState = SkillPanelState.Normal;
    private string currentPanelName = "Normal";

    public string CurrentGroupName 
    {
        get => currentPanelName;
        private set => currentPanelName = value;
    }

    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        InitializeSkillPanelUI(); // Инициализируем UI через менеджер
        SetupInitialSkillPanels();
    }

    private void InitializeSkillPanelUI()
    {
        // Получаем объект PanelSwitcher через UIManager
        GameObject panelSwitcher = uiManager.GetPanel(UIManager.PanelType.Selection);
        if (panelSwitcher != null)
        {
            skillPanelUI = panelSwitcher.GetComponent<ISkillPanelUI>();
            if (skillPanelUI == null)
            {
                Debug.LogError("SkillPanelUI не найден на объекте PanelSwitcher!");
                return;
            }
            (skillPanelUI as SkillPanelUI)?.Initialize(this); // Инициализируем UI
        }
        else
        {
            Debug.LogError("PanelSwitcher (Selection) не найден в UIManager!");
        }
    }

    private void SetupInitialSkillPanels()
    {
        foreach (var panel in panels)
        {
            panel.panelObject.SetActive(false);
        }
        SetInitialPanelState();
    }

    public void SetInitialPanelState()
    {
        foreach (var panel in panels)
        {
            panel.panelObject.SetActive(false);
        }

        panels[0].panelObject.SetActive(true);
        currentSkillState = SkillPanelState.Normal;
        CurrentGroupName = "Normal";

        skillTreeNavigation?.ResetNavigation();
        skillPanelUI?.UpdateSkillsButtonText();
    }

    public void SwitchToPanel(PanelData panel)
    {
        if (panel == null)
        {
            Debug.LogError("Panel is null. Cannot switch to panel.");
            return;
        }

        if (CurrentGroupName == panel.panelName)
        {
            Debug.Log("Selected panel is already active. No switch needed.");
            return;
        }

        panel.panelObject.SetActive(true);
        CurrentGroupName = panel.panelName;
        currentSkillState = (SkillPanelState)System.Enum.Parse(typeof(SkillPanelState), panel.panelName);

        foreach (var otherPanel in panels)
        {
            if (otherPanel != panel)
            {
                otherPanel.panelObject.SetActive(false);
            }
        }

        skillTreeNavigation?.ResetNavigation();
        skillPanelUI?.UpdateSkillsButtonText();
    }

    public string GetCurrentPanelName()
    {
        return CurrentGroupName;
    }

    public List<PanelData> GetPanels()
    {
        return panels;
    }
}
[System.Serializable]
public class PanelData
{
    public string panelName;
    public GameObject panelObject;
    public bool isSelectable = true; // По умолчанию панель доступна в Selection
}