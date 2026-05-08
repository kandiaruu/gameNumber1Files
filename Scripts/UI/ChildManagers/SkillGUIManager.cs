using UnityEngine;

//
// Manages the Skill GUI panel: resolves it via UIManager and exposes
// open, close, and update operations for displaying per-skill detail views.
//

public class SkillGuiManager : MonoBehaviour, ISkillGuiManager
{
    [InjectAttribute1]
    private ISkillTreeManager skillTreeManager { get; set; }
    [InjectAttribute1]
    private ISkillGuiPanel skillGuiPanel { get; set; }
    [InjectAttribute1]
    private IUIManager uiManager { get; set; }

    // Injects dependencies and initializes the SkillGui panel reference
    void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        InitializeSkillGuiPanel();
    }

    // Retrieves the SkillGui panel from UIManager and caches the ISkillGuiPanel component
    private void InitializeSkillGuiPanel()
    {
        GameObject skillGuiPanelObject = uiManager.GetPanel(UIManager.PanelType.SkillGui);
        if (skillGuiPanelObject != null)
        {
            skillGuiPanel = skillGuiPanelObject.GetComponent<SkillGuiPanel>();
            if (skillGuiPanel == null)
            {
                Debug.LogError($"Panel {skillGuiPanelObject.name} (SkillGui) does not implement ISkillGuiPanel!");
            }
        }
        else
        {
            Debug.LogError("Panel of type SkillGui not found in UIManager cache!");
        }
    }

    // Opens the Skill GUI and displays information for the given skill
    public void ShowSkillGui(Skill skill)
    {
        if (skillGuiPanel != null)
        {
            skillGuiPanel.ShowSkillGui(skill);
        }
        else
        {
            Debug.LogWarning("SkillGuiPanel is not initialized!");
        }
    }

    // Returns true if the Skill GUI panel is currently open
    public bool IsSkillGuiActive()
    {
        return skillGuiPanel != null && skillGuiPanel.IsOpen;
    }

    // Closes the Skill GUI panel if it is currently open
    public void CloseSkillGui()
    {
        if (skillGuiPanel != null && skillGuiPanel.IsOpen)
        {
            skillGuiPanel.Close();
        }
    }

    // Refreshes the Skill GUI panel's displayed data if it is open
    public void UpdateUI()
    {
        if (skillGuiPanel != null && skillGuiPanel.IsOpen)
        {
            skillGuiPanel.UpdateUI();
        }
    }
}
