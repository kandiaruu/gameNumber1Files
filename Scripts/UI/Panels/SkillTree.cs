//
// Root panel for the skill tree UI. On close it resets the skill panel
// back to its initial state and hides any open tooltip.
//

public class SkillTree : BasePanel, ISkillTree
{
    [InjectAttribute1] private ISkillPanelManager skillPanelManager { get; set; }
    [InjectAttribute1] private ITooltipManager tooltipManager { get; set; }

    // Injects dependencies when the object wakes up
    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    // Closes the panel, resets the skill panel to its default state, and hides any visible tooltip
    public override void Close()
    {
        base.Close();
        skillPanelManager.SetInitialPanelState();
        tooltipManager.HideTooltip();
    }
}
