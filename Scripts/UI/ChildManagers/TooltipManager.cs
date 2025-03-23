using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    // Удаляем Singleton
    // public static TooltipManager Instance { get; private set; }

    private GameObject tooltipPanelObject;
    private SkillTooltipPanel tooltip;
    private int initAttempts = 0;
    private const int MAX_INIT_ATTEMPTS = 5;
    private UIManager uiManager; // Зависимость через DI

    void Awake()
    {
        // Удаляем логику Singleton
        // Получаем UIManager через DependencyContainer
        uiManager = DependencyContainer.Instance.Resolve<UIManager>();
        if (uiManager == null) Debug.LogError("UIManager не зарегистрирован в DependencyContainer!");

        // Изменение: используем UnityEngine.Object для DontDestroyOnLoad
        UnityEngine.Object.DontDestroyOnLoad(gameObject);

        InitializeTooltip();
    }

    private void InitializeTooltip()
    {
        if (uiManager == null)
        {
            if (initAttempts >= MAX_INIT_ATTEMPTS)
            {
                Debug.LogError("Не удалось инициализировать TooltipManager: UIManager так и не был найден после максимального числа попыток!");
                return;
            }
            initAttempts++;
            Debug.LogWarning($"UIManager не доступен, откладываем инициализацию (попытка {initAttempts}/{MAX_INIT_ATTEMPTS})...");
            Invoke(nameof(InitializeTooltip), 0.1f);
            return;
        }

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