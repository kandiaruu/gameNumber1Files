using UnityEngine;

public class TooltipManager : MonoBehaviour, ITooltipManager
{
    private GameObject tooltipPanelObject;
    [InjectAttribute1]
    private ISkillTooltipPanel tooltip { get; set; }
    [InjectAttribute1]
    private IUIManager uiManager { get; set; }

    void Awake()
    {
        DependencyContainer1.InjectDependencies(this);

        transform.SetParent(null);
        UnityEngine.Object.DontDestroyOnLoad(gameObject);

        InitializeTooltip();
    }

    private void InitializeTooltip()
    {
        tooltipPanelObject = uiManager.GetPanel(UIManager.PanelType.Tooltip);
        if (tooltipPanelObject != null)
        {
            tooltip = tooltipPanelObject.GetComponent<SkillTooltipPanel>();
            if (tooltip == null)
            {
                Debug.LogError($"Панель {tooltipPanelObject.name} (Tooltip) не реализует SkillTooltipPanel!");
            }
        }
        else
        {
            Debug.LogError("Панель типа Tooltip не найдена в кэше UIManager!");
        }
    }

    public void ShowTooltip(Skill skill, Vector3 mousePosition)
    {
        if (tooltip != null)
        {
            tooltip.ShowTooltip(skill, mousePosition);
        }
        else
        {
            Debug.LogWarning("Tooltip не инициализирован!");
        }
    }

    public void ShowTooltip(string content, Vector3 mousePosition)
    {
        if (tooltip != null)
        {
            tooltip.ShowTooltip(content, mousePosition);
        }
        else
        {
            Debug.LogWarning("Tooltip не инициализирован!");
        }
    }

    public void HideTooltip()
    {
        if (tooltip != null && tooltip.IsOpen)
        {
            tooltipPanelObject.GetComponent<IPanel>()?.Close();
        }
    }

    public void UpdatePosition(Vector3 mousePosition)
    {
        if (tooltip != null && tooltip.IsOpen)
        {
            tooltip.UpdatePosition(mousePosition);
        }
    }

    public bool IsOpen => tooltip != null && tooltip.IsOpen;
}