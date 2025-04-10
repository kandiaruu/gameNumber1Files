using UnityEngine;
using System.Collections.Generic;

public class SkillTreeNavigation : MonoBehaviour, ISkillTreeNavigation
{
    [SerializeField] private RectTransform skillHolder;
    [SerializeField] private RectTransform skillTreeContainer;
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 3f;
    [SerializeField] private float edgeMoveSpeed = 500f;

    private Vector3 dragOrigin;
    public bool isDragging { get; private set; } = false;
    private Vector2 originalPivot;
    private bool dragStartedInContainer = false;

    [InjectAttribute1] private IUIManager uiManager { get; set; }

    [InjectAttribute1] private ISkillNotificationHandler notificationHandler { get; set; }

    public string CurrentGroupName { get; private set; } = "Normal";

    private struct PanelStateData
    {
        public Vector2 Position;
        public Vector3 Scale;
    }
    private Dictionary<SkillPanelManager.SkillPanelState, PanelStateData> panelStates = new();

    private void Awake()
    {
        // DependencyContainer1.InjectDependencies(this);

        // panelStates[SkillPanelManager.SkillPanelState.Normal] = new PanelStateData
        // {
        //     Position = new Vector2(960f, -455f),
        //     Scale = Vector3.one
        // };
        // panelStates[SkillPanelManager.SkillPanelState.Hidden] = new PanelStateData
        // {
        //     Position = new Vector2(960f, -455f),
        //     Scale = Vector3.one
        // };

        // skillHolder.anchoredPosition = panelStates[SkillPanelManager.SkillPanelState.Normal].Position;
        // skillHolder.localScale = panelStates[SkillPanelManager.SkillPanelState.Normal].Scale;
        originalPivot = skillHolder.pivot;
    }

    private void Update()
    {
        if (skillHolder == null || skillTreeContainer == null)
        {
            Debug.LogError("skillHolder or skillTreeContainer is null!");
            return;
        }

        bool isMouseOverContainer = RectTransformUtility.RectangleContainsScreenPoint(skillTreeContainer, Input.mousePosition);

        // Проверяем, разрешена ли навигация (зум и перетаскивание)
        bool allowNavigation = uiManager.ShouldAllowNavigation();

        if (allowNavigation && isMouseOverContainer)
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
                Debug.Log("Drag started");
            }
        }

        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
        {
            isDragging = false;
            dragStartedInContainer = false;
            Debug.Log("Drag ended");
        }

        if (allowNavigation && isDragging && dragStartedInContainer)
        {
            Vector3 delta = Input.mousePosition - dragOrigin;
            dragOrigin = Input.mousePosition;
            Vector2 newPosition = skillHolder.anchoredPosition + new Vector2(delta.x, delta.y);
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
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(skillHolder, mouseScreenPos, null, out localPoint);

        Vector2 pivotPosition = new Vector2(localPoint.x / skillHolder.rect.width, localPoint.y / skillHolder.rect.height);

        skillHolder.pivot = pivotPosition;
        skillHolder.localScale = new Vector3(newScale, newScale, 1f);

        skillHolder.pivot = originalPivot;

        Vector2 newPosition = skillHolder.anchoredPosition - localPoint * (newScale - currentScale);
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

    public void LoadPanelState(SkillPanelManager.SkillPanelState state)
    {
        if (panelStates.ContainsKey(state))
        {
            skillHolder.anchoredPosition = panelStates[state].Position;
            skillHolder.localScale = panelStates[state].Scale;
        }
    }

    public void SetCurrentGroup(string groupName)
    {
        CurrentGroupName = groupName;
        Debug.Log($"SkillTreeNavigation: Current group set to {CurrentGroupName}");
        LoadPanelState((SkillPanelManager.SkillPanelState)System.Enum.Parse(typeof(SkillPanelManager.SkillPanelState), groupName));
    }

    public void ResetNavigation()
    {
        skillHolder.anchoredPosition = new Vector2(960f, -455f);
        skillHolder.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        isDragging = false;
        dragStartedInContainer = false;
    }
}