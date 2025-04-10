using System.Collections.Generic;

public interface ISkillPanelManager
{
    public enum SkillPanelState
    {
        Normal,
        Hidden
    }

    //SkillPanelState GetCurrentSkillState();
    string GetCurrentPanelName();
    List<PanelData> GetPanels();
    void SwitchToPanel(PanelData panel);
    void SetInitialPanelState();
}