using System.Collections.Generic;

public interface ISkillPanelManager
{
    string GetCurrentPanelName();
    List<PanelData> GetPanels();
    void SwitchToPanel(PanelData panel);
    void SetInitialPanelState();
    void toggleSelection();
}