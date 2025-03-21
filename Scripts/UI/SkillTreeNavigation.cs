using UnityEngine;

public class SkillTreeNavigation : MonoBehaviour
{
    [SerializeField] private RectTransform skillHolder;
    [SerializeField] private RectTransform skillTreeContainer;
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 3f;

    private Vector3 dragOrigin;
    public bool isDragging { get; private set; } = false;
    private Vector2 originalPivot;
    private bool dragStartedInContainer = false;

    [SerializeField] private SkillTreeManager skillTreeManager; // Ссылка на SkillTreeManager

    private void Awake()
    {
        if (skillHolder == null || skillTreeContainer == null || skillTreeManager == null)
        {
            Debug.LogError("SkillHolder, SkillTreeContainer или SkillTreeManager не назначены!");
            enabled = false;
            return;
        }
        skillHolder.anchoredPosition = new Vector2(960f, -455f);
        originalPivot = skillHolder.pivot;
        Debug.Log("Инициализация завершена. Начальная позиция: " + skillHolder.anchoredPosition);
    }

    private void Update()
    {
        // Проверяем, находится ли курсор мыши над областью SkillTreeContainer и уведомление не активно
        bool isMouseOverContainer = RectTransformUtility.RectangleContainsScreenPoint(skillTreeContainer, Input.mousePosition);
        bool isNotificationActive = skillTreeManager != null && skillTreeManager.skillNotificationPanel.activeSelf;

        if (!isNotificationActive && isMouseOverContainer)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f && !Input.GetMouseButton(2))
            {
                HandleZoom(scrollInput);
            }

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
            {
                isDragging = true;
                dragStartedInContainer = true;
                dragOrigin = Input.mousePosition;
                Debug.Log("Перетаскивание начато внутри контейнера.");
            }
        }

        // Отпускание кнопки в любом месте экрана завершает перетаскивание
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
        {
            isDragging = false;
            dragStartedInContainer = false;
            Debug.Log("Перетаскивание завершено.");
        }

        // Обработка перетаскивания, если оно началось внутри контейнера и уведомление не активно
        if (!isNotificationActive && isDragging && dragStartedInContainer)
        {
            Vector3 delta = Input.mousePosition - dragOrigin;
            dragOrigin = Input.mousePosition;
            Vector3 newPosition = skillHolder.anchoredPosition + new Vector2(delta.x, delta.y);
            skillHolder.anchoredPosition = ClampPosition(newPosition);
        }
    }

    private void HandleZoom(float scrollDelta)
    {
        float currentScale = skillHolder.localScale.x;
        float zoomDelta = scrollDelta * zoomSpeed;
        float newScale = Mathf.Clamp(currentScale + zoomDelta, minZoom, maxZoom);

        if (Mathf.Approximately(newScale, currentScale)) return;

        Vector2 mouseScreenPos = Input.mousePosition;
        Vector2 mouseLocalPosBefore;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(skillHolder, mouseScreenPos, null, out mouseLocalPosBefore);

        Vector2 currentPosition = skillHolder.anchoredPosition;
        Vector2 mouseRelativeToAnchor = mouseLocalPosBefore / currentScale;

        skillHolder.localScale = new Vector3(newScale, newScale, 1f);
        Vector2 mouseLocalPosAfter = mouseRelativeToAnchor * newScale;
        Vector2 positionDelta = mouseLocalPosAfter - mouseLocalPosBefore;
        Vector2 newPosition = currentPosition - positionDelta;

        skillHolder.anchoredPosition = ClampPosition(newPosition);
    }

    private Vector2 ClampPosition(Vector2 position)
    {
        Vector2 holderSize = skillHolder.rect.size * skillHolder.localScale.x;
        float containerWidth = skillTreeContainer.rect.width;
        float containerHeight = skillTreeContainer.rect.height;

        float minX = -holderSize.x * 0.5f + containerWidth * 0.5f;
        float maxX = containerWidth * 0.5f + holderSize.x * 0.5f;
        float minY = -containerHeight * 0.5f - holderSize.y * 0.5f;
        float maxY = holderSize.y * 0.5f - containerHeight * 0.5f;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    public void ResetNavigation()
    {
        skillHolder.anchoredPosition = Vector2.zero;
        skillHolder.localScale = Vector3.one;
        Debug.Log("Навигация сброшена.");
    }
}