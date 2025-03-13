using UnityEngine;

public class SkillTreeNavigation : MonoBehaviour
{
    [SerializeField] private RectTransform skillHolder;
    [SerializeField] private RectTransform skillTreeContainer;
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 3f;

    private Vector3 dragOrigin;
    public bool isDragging { get; private set; } = false; // Публичный геттер для доступа из SkillTreeManager
    private Vector2 originalPivot;

    private void Awake()
    {
        if (skillHolder == null || skillTreeContainer == null)
        {
            Debug.LogError("SkillHolder или SkillTreeContainer не назначены!");
            enabled = false;
            return;
        }
        skillHolder.anchoredPosition = new Vector2(960f, -540f); // Устанавливаем начальную позицию в (960, -540)
        originalPivot = skillHolder.pivot;
        Debug.Log("Инициализация завершена. Начальная позиция: " + skillHolder.anchoredPosition);
    }

    private void Update()
    {
        // Проверяем, находится ли курсор мыши над областью SkillTreeContainer
        bool isMouseOverContainer = RectTransformUtility.RectangleContainsScreenPoint(skillTreeContainer, Input.mousePosition);

        if (isMouseOverContainer)
        {
            // Обработка прокрутки колесика для масштабирования
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f && !Input.GetMouseButton(2)) // Прокрутка только если средняя кнопка не зажата
            {
                HandleZoom(scrollInput);
            }

            // Перемещение с левой кнопкой мыши или средней кнопкой (колесиком)
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2)) // 0 — левая, 2 — средняя
            {
                isDragging = true;
                dragOrigin = Input.mousePosition;
            }
        }

        // Проверяем отпускание кнопки в любом месте экрана
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
        {
            isDragging = false;
            Debug.Log("Перетаскивание завершено.");
        }

        // Обработка перетаскивания
        if (isDragging)
        {
            // Если курсор вышел за пределы контейнера, прекращаем перетаскивание
            if (!isMouseOverContainer)
            {
                isDragging = false;
                return;
            }

            Vector3 delta = Input.mousePosition - dragOrigin;
            dragOrigin = Input.mousePosition;
            Vector3 newPosition = skillHolder.anchoredPosition + new Vector2(delta.x, delta.y);
            skillHolder.anchoredPosition = ClampPosition(newPosition);
        }
    }

    private void HandleZoom(float scrollDelta)
    {
        // Текущий масштаб
        float currentScale = skillHolder.localScale.x;

        // Новый масштаб
        float zoomDelta = scrollDelta * zoomSpeed;
        float newScale = Mathf.Clamp(currentScale + zoomDelta, minZoom, maxZoom);

        // Если масштаб не изменился, выходим
        if (Mathf.Approximately(newScale, currentScale)) return;

        // Позиция курсора в экранных координатах
        Vector2 mouseScreenPos = Input.mousePosition;

        // Преобразуем позицию курсора в локальные координаты skillHolder до масштабирования
        Vector2 mouseLocalPosBefore;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(skillHolder, mouseScreenPos, null, out mouseLocalPosBefore);

        // Текущая позиция skillHolder
        Vector2 currentPosition = skillHolder.anchoredPosition;

        // Вычисляем позицию точки под курсором относительно anchoredPosition с учётом текущего масштаба
        Vector2 mouseRelativeToAnchor = mouseLocalPosBefore / currentScale;

        // Применяем новый масштаб
        skillHolder.localScale = new Vector3(newScale, newScale, 1f);

        // Вычисляем новую позицию точки под курсором после масштабирования
        Vector2 mouseLocalPosAfter = mouseRelativeToAnchor * newScale;

        // Корректируем позицию, чтобы точка под курсором осталась неподвижной
        Vector2 positionDelta = mouseLocalPosAfter - mouseLocalPosBefore;
        Vector2 newPosition = currentPosition - positionDelta;

        // Применяем новую позицию с ограничением
        skillHolder.anchoredPosition = ClampPosition(newPosition);
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