public interface ISkillPanelUI : IPanel
{
    void UpdateSkillsButtonText();
    void UnlockPanel(string panelName);
    void ClearPanelButtons();
    void CreatePanelButtons();
    void TogglePanelSelection();
}