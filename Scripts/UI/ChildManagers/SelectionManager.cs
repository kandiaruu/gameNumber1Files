using UnityEngine;
using System.Collections.Generic;

//
// Manages which skill panel group is active (e.g. Normal vs Hidden).
// Resolves the UI switcher via UIManager, handles panel switching logic,
// and keeps the navigation state in sync.
//

public class SkillPanelManager : MonoBehaviour, ISkillPanelManager
{
    public enum SkillPanelState
    {
        Normal,
        Hidden
    }

    [InjectAttribute1] private ISkillPanelUI skillPanelUI { get; set; }
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1] private IUIManager uiManager { get; set; }
    [SerializeField] private List<PanelData> panels;

    private SkillPanelState currentSkillState = SkillPanelState.Normal;
    private string currentPanelName = "Normal";

    // The name of the currently active panel group
    public string CurrentGroupName
    {
        get => currentPanelName;
        private set => currentPanelName = value;
    }

    // Injects dependencies, resolves the panel UI, and sets up the initial panel state
    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        InitializeSkillPanelUI();
        SetupInitialSkillPanels();
    }

    // Retrieves the Selection panel from UIManager and wires up the ISkillPanelUI component
    private void InitializeSkillPanelUI()
    {
        GameObject panelSwitcher = uiManager.GetPanel(UIManager.PanelType.Selection);
        if (panelSwitcher != null)
        {
            skillPanelUI = panelSwitcher.GetComponent<ISkillPanelUI>();
            if (skillPanelUI == null)
            {
                Debug.LogError("SkillPanelUI not found on the PanelSwitcher object!");
                return;
            }
            (skillPanelUI as SkillPanelUI)?.Initialize(this);
        }
        else
        {
            Debug.LogError("PanelSwitcher (Selection) not found in UIManager!");
        }
    }

    // Deactivates all panels and then activates only the first (default) panel
    private void SetupInitialSkillPanels()
    {
        foreach (var panel in panels)
        {
            panel.panelObject.SetActive(false);
        }
        SetInitialPanelState();
    }

    // Resets all panels to their default state: only the first panel is active
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

    // Activates the given panel, deactivates all others, and updates navigation and UI buttons
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

        if (panel.panelName == "Hidden")
        {
            skillTreeNavigation.CenterOnSkill(skillTreeNavigation.getLastSkill());
        }
        else
        {
            skillTreeNavigation.ResetNavigation();
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

        skillPanelUI?.UpdateSkillsButtonText();
        skillPanelUI?.ClearPanelButtons();
        skillPanelUI?.CreatePanelButtons();
    }

    // Toggles the panel selection UI highlight state
    public void toggleSelection()
    {
        skillPanelUI?.TogglePanelSelection();
    }

    // Returns the name of the currently active panel group
    public string GetCurrentPanelName()
    {
        return CurrentGroupName;
    }

    // Returns the full list of registered panel data entries
    public List<PanelData> GetPanels()
    {
        return panels;
    }
}

// Data container describing a single skill panel group entry
[System.Serializable]
public class PanelData
{
    public string panelName;
    public GameObject panelObject;
    public bool isSelectable = true;
}
