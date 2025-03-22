using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    private GameObject tooltipPanelObject;
    private ISkillTooltip tooltip;

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

        InitializeTooltip();
    }

    private void InitializeTooltip()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager ещё не инициализирован, откладываем инициализацию TooltipManager...");
            Invoke(nameof(InitializeTooltip), 0.1f); // Отложенная инициализация
            return;
        }

        // Явно указываем UIManager.PanelType
        tooltipPanelObject = UIManager.Instance.GetPanel(UIManager.PanelType.Tooltip);
        if (tooltipPanelObject != null)
        {
            tooltip = tooltipPanelObject.GetComponent<ISkillTooltip>();
            if (tooltip == null)
            {
                Debug.LogError($"Панель {tooltipPanelObject.name} (Tooltip) не реализует ISkillTooltip!");
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

    public void HideTooltip()
    {
        if (tooltip != null && tooltip.IsOpen)
        {
            tooltipPanelObject.GetComponent<IPanel>()?.Close();
        }
    }

    public void UpdateTooltipPosition(Vector3 mousePosition)
    {
        if (tooltip != null && tooltip.IsOpen)
        {
            tooltip.UpdatePosition(mousePosition);
        }
    }
}