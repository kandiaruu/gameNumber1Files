using UnityEngine;
using TMPro;

public class SkillTooltipPanel : BasePanel, ISkillTooltipPanel
{
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private float offsetX = 375f;
    private RectTransform tooltipRect;

    public override void Awake()
    {
        base.Awake();
        tooltipRect = GetComponent<RectTransform>();
        if (tooltipText == null) Debug.LogError("tooltipText не назначен!");
        if (tooltipRect == null) Debug.LogError("tooltipRect не найден!");
    }

    public void ShowTooltip(Skill skill, Vector3 mousePosition)
    {
        Open();
        string content = $"Навык: {skill.skillName}\n" +
                         $"Описание: {skill.description}\n" +
                         $"Характеристики: {string.Join(", ", skill.characteristics ?? new string[] { "Нет данных" })}\n" +
                         $"Макс. улучшений: {skill.maxUpgrades}";
        tooltipText.text = content;

        Vector3 targetPosition = new Vector3(mousePosition.x + offsetX, mousePosition.y, 0f);
        Vector2 tooltipSize = tooltipRect.sizeDelta;
        if (targetPosition.x + tooltipSize.x > Screen.width)
            targetPosition.x = Screen.width - tooltipSize.x;
        targetPosition.y = Mathf.Clamp(targetPosition.y, tooltipSize.y, Screen.height);
        tooltipRect.position = targetPosition;

        Canvas.ForceUpdateCanvases();
    }

    public void UpdatePosition(Vector3 mousePosition)
    {
        Vector3 targetPosition = new Vector3(mousePosition.x + offsetX, mousePosition.y, 0f);
        Vector2 tooltipSize = tooltipRect.sizeDelta;
        if (targetPosition.x + tooltipSize.x > Screen.width)
            targetPosition.x = Screen.width - tooltipSize.x;
        targetPosition.y = Mathf.Clamp(targetPosition.y, tooltipSize.y, Screen.height);

        tooltipRect.position = Vector3.Lerp(tooltipRect.position, targetPosition, Time.unscaledDeltaTime * 15f);
    }
}