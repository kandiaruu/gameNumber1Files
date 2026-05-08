using UnityEngine;

//
// Manages the skill tooltip lifecycle: resolves the tooltip panel via UIManager,
// and exposes Show, Hide, and UpdatePosition operations for use by other systems.
//

public class TooltipManager : MonoBehaviour, ITooltipManager
{
    private GameObject tooltipPanelObject;
    [InjectAttribute1]
    private ISkillTooltipPanel tooltip { get; set; }
    [InjectAttribute1]
    private IUIManager uiManager { get; set; }

    // Injects dependencies, detaches from any parent, and persists across scene loads
    void Awake()
    {
        DependencyContainer1.InjectDependencies(this);

        transform.SetParent(null);
        UnityEngine.Object.DontDestroyOnLoad(gameObject);

        InitializeTooltip();
    }

    // Retrieves the Tooltip panel from UIManager and caches the ISkillTooltipPanel component
    private void InitializeTooltip()
    {
        tooltipPanelObject = uiManager.GetPanel(UIManager.PanelType.Tooltip);
        if (tooltipPanelObject != null)
        {
            tooltip = tooltipPanelObject.GetComponent<SkillTooltipPanel>();
            if (tooltip == null)
            {
                Debug.LogError($"Panel {tooltipPanelObject.name} (Tooltip) does not implement SkillTooltipPanel!");
            }
        }
        else
        {
            Debug.LogError("Panel of type Tooltip not found in UIManager cache!");
        }
    }

    // Displays a skill tooltip at the given screen position
    public void ShowTooltip(Skill skill, Vector3 mousePosition)
    {
        if (tooltip != null)
        {
            tooltip.ShowTooltip(skill, mousePosition);
        }
        else
        {
            Debug.LogWarning("Tooltip is not initialized!");
        }
    }

    // Displays a plain-text tooltip at the given screen position
    public void ShowTooltip(string content, Vector3 mousePosition)
    {
        if (tooltip != null)
        {
            tooltip.ShowTooltip(content, mousePosition);
        }
        else
        {
            Debug.LogWarning("Tooltip is not initialized!");
        }
    }

    // Closes the tooltip panel if it is currently open
    public void HideTooltip()
    {
        if (tooltip != null && tooltip.IsOpen)
        {
            tooltipPanelObject.GetComponent<IPanel>()?.Close();
        }
    }

    // Updates the tooltip's screen position to follow the mouse cursor
    public void UpdatePosition(Vector3 mousePosition)
    {
        if (tooltip != null && tooltip.IsOpen)
        {
            tooltip.UpdatePosition(mousePosition);
        }
    }

    // Returns true if the tooltip panel is currently visible
    public bool IsOpen => tooltip != null && tooltip.IsOpen;
}
