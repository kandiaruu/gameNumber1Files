using UnityEngine;

public class SkillTreeNavigation : MonoBehaviour
{
    [SerializeField] private RectTransform skillHolder;
    [SerializeField] private RectTransform skillTreeContainer;
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 3f;

    private Vector3 dragOrigin;
    private bool isDragging = false;
    private Vector2 originalPivot;

    private void Awake()
    {
        if (skillHolder == null || skillTreeContainer == null)
        {
            Debug.LogError("SkillHolder или SkillTreeContainer не назначены!");
            enabled = false;
            return;
        }
        skillHolder.anchoredPosition = Vector2.zero;
        originalPivot = skillHolder.pivot;
        Debug.Log("Инициализация завершена.");
    }

    private void Update()
    {
        // Обработка прокрутки колесика для масштабирования
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f && !Input.GetMouseButton(2)) // Прокрутка только если средняя кнопка не зажата
        {
            Debug.Log("Прокрутка для масштабирования: " + scrollInput);
            HandleZoom(scrollInput);
        }

        // Перемещение с левой кнопкой мыши или средней кнопкой (колесиком)
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2)) // 0 — левая, 2 — средняя
        {
            isDragging = true;
            dragOrigin = Input.mousePosition;
            Debug.Log("Начало перемещения. Начальная позиция: " + dragOrigin);
        }
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
        {
            isDragging = false;
            Debug.Log("Конец перемещения");
        }
        if (isDragging)
        {
            Vector3 delta = Input.mousePosition - dragOrigin;
            dragOrigin = Input.mousePosition;
            Vector3 newPosition = skillHolder.anchoredPosition + new Vector2(delta.x, delta.y);
            skillHolder.anchoredPosition = ClampPosition(newPosition);
            Debug.Log("Перемещение: Delta = " + delta + ", Новая позиция = " + newPosition);
        }
    }

    private void HandleZoom(float scrollDelta)
    {
        float zoomDelta = scrollDelta * zoomSpeed;
        float newScaleValue = skillHolder.localScale.x + zoomDelta;
        newScaleValue = Mathf.Clamp(newScaleValue, minZoom, maxZoom);
        Vector3 newScale = new Vector3(newScaleValue, newScaleValue, 1f);

        Vector2 mousePosBeforeZoom;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(skillHolder, Input.mousePosition, null, out mousePosBeforeZoom);
        skillHolder.localScale = newScale;
        Vector2 mousePosAfterZoom;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(skillHolder, Input.mousePosition, null, out mousePosAfterZoom);

        Vector2 delta = (mousePosAfterZoom - mousePosBeforeZoom) * newScale.x;
        skillHolder.anchoredPosition = ClampPosition(skillHolder.anchoredPosition - delta);
    }

    private Vector2 ClampPosition(Vector2 position)
    {
        Vector2 holderSize = skillHolder.rect.size * skillHolder.localScale.x;
        float containerWidth = skillTreeContainer.rect.width;
        float containerHeight = skillTreeContainer.rect.height;

        float minX = -(holderSize.x * 0.5f - containerWidth);
        float maxX = holderSize.x * 0.5f;
        float minY = -holderSize.y * 0.5f;
        float maxY = (holderSize.y * 0.5f) - containerHeight;

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