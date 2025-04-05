using UnityEngine;
using TMPro;

public class SkillTooltipPanel : BasePanel, ISkillTooltipPanel
{
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private float offsetX = 375f;
    [SerializeField] private RectTransform skillTreeContainer;
    [SerializeField] private float moveSpeed = 10f; // Скорость перемещения
    private RectTransform tooltipRect;
    private Vector3 targetPosition;
    private bool isMoving = false;

    public override void Awake()
    {
        base.Awake();
        tooltipRect = GetComponent<RectTransform>();
        if (tooltipText == null) Debug.LogError("tooltipText не назначен!");
        if (tooltipRect == null) Debug.LogError("tooltipRect не найден!");
        if (skillTreeContainer == null) Debug.LogError("skillTreeContainer не назначен!");
    }

    private void Update()
    {
        if (isMoving)
        {
            tooltipRect.position = Vector3.Lerp(tooltipRect.position, targetPosition, moveSpeed * Time.unscaledDeltaTime);
            
            if (Vector3.Distance(tooltipRect.position, targetPosition) < 0.1f)
            {
                tooltipRect.position = targetPosition;
                isMoving = false;
            }
        }
    }

    public void ShowTooltip(Skill skill, Vector3 mousePosition)
    {
        Open();
        string content = $"Навык: {skill.skillName}\n" +
                         $"Описание: {skill.description}\n" +
                         $"Характеристики: {string.Join(", ", skill.characteristics ?? new string[] { "Нет данных" })}\n" +
                         $"Макс. улучшений: {skill.maxUpgrades}";
        tooltipText.text = content;

        // При первом появлении телепортируемся мгновенно
        SetTooltipPosition(mousePosition, true);
    }

    public void UpdatePosition(Vector3 mousePosition)
    {
        // После появления используем плавное перемещение
        SetTooltipPosition(mousePosition, false);
    }

    private void SetTooltipPosition(Vector3 mousePosition, bool instantMove = false)
    {
        Vector3 newTargetPosition = new Vector3(mousePosition.x + offsetX, mousePosition.y, 0f);
        Vector2 tooltipSize = tooltipRect.rect.size;

        // Получаем границы skillTreeContainer в мировых координатах
        Vector3[] containerCorners = new Vector3[4];
        skillTreeContainer.GetWorldCorners(containerCorners);
        
        // Горизонтальные границы
        float containerRight = containerCorners[2].x - (tooltipSize.x / 2);
        float containerLeft = containerCorners[0].x + (tooltipSize.x / 2);
        
        // Вертикальные границы
        float containerTop = containerCorners[1].y - (tooltipSize.y / 2);
        float containerBottom = containerCorners[0].y + (tooltipSize.y / 2);

        // Проверяем горизонтальные границы
        if (newTargetPosition.x > containerRight)
        {
            newTargetPosition.x = containerRight;
        }
        if (newTargetPosition.x < containerLeft)
        {
            newTargetPosition.x = containerLeft;
        }

        // Проверяем вертикальные границы
        if (newTargetPosition.y > containerTop)
        {
            newTargetPosition.y = containerTop;
        }
        if (newTargetPosition.y < containerBottom)
        {
            newTargetPosition.y = containerBottom;
        }

        if (instantMove)
        {
            tooltipRect.position = newTargetPosition;
            targetPosition = newTargetPosition;
            isMoving = false;
        }
        else
        {
            targetPosition = newTargetPosition;
            isMoving = true;
        }
    }
}