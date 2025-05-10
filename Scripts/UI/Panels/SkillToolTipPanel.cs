using UnityEngine;
using TMPro;

public class SkillTooltipPanel : BasePanel, ISkillTooltipPanel
{
    
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private float offsetX = 375f;
    [SerializeField] private RectTransform skillTreeContainer;
    [SerializeField] private float moveSpeed = 10f;
    private RectTransform tooltipRect;
    private Vector3 targetPosition;
    private bool isMoving = false;
    public bool InventoryToolTip = false;

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

        SetTooltipPosition(mousePosition, true, false);
        InventoryToolTip = false;
    }

    public void ShowTooltip(string content, Vector3 mousePosition)
    {
        Open();
        tooltipText.text = content;
        SetTooltipPosition(mousePosition, true, true);
        InventoryToolTip = true;
    }

    public void UpdatePosition(Vector3 mousePosition)
    {   
        if (InventoryToolTip) 
        {
            SetTooltipPosition(mousePosition, false, true);
        }
        else 
        {
            SetTooltipPosition(mousePosition, false, false);
        }
    }

    private void SetTooltipPosition(Vector3 mousePosition, bool instantMove = false, bool InventoryToolTip = false)
    {
        Vector2 tooltipSize = tooltipRect.rect.size;

        Vector3 newTargetPosition;
        
        if (InventoryToolTip) {
            newTargetPosition = new Vector3(
                mousePosition.x + offsetX + tooltipSize.x / 2f,
                mousePosition.y + tooltipSize.y / 2f,
                0f
            );
        }
        else
        {
            newTargetPosition = new Vector3(
            mousePosition.x + offsetX + tooltipSize.x / 2f,
            mousePosition.y - tooltipSize.y / 2f,
            0f
            );
        }

        Vector3[] containerCorners = new Vector3[4];
        skillTreeContainer.GetWorldCorners(containerCorners);

        float containerLeft = containerCorners[0].x;
        float containerRight = containerCorners[2].x;
        float containerTop = containerCorners[1].y;
        float containerBottom = containerCorners[0].y;

        float halfWidth = tooltipSize.x / 2f;
        float halfHeight = tooltipSize.y / 2f;

        if (newTargetPosition.x + halfWidth > containerRight)
            newTargetPosition.x = containerRight - halfWidth;
        if (newTargetPosition.x - halfWidth < containerLeft)
            newTargetPosition.x = containerLeft + halfWidth;

        if (newTargetPosition.y + halfHeight > containerTop)
            newTargetPosition.y = containerTop - halfHeight;
        if (newTargetPosition.y - halfHeight < containerBottom)
            newTargetPosition.y = containerBottom + halfHeight;

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