using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

//
// Handles mouse-driven panning and scroll-wheel zooming of the skill tree canvas.
// Also provides programmatic centering on a specific skill and navigation reset.
//

public class SkillTreeNavigation : MonoBehaviour, ISkillTreeNavigation
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

    [InjectAttribute1] private IUIManager uiManager { get; set; }

    private struct PanelStateData
    {
        public Vector2 Position;
        public Vector3 Scale;
    }
    private Dictionary<SkillPanelManager.SkillPanelState, PanelStateData> panelStates = new();

    public Skill LastSkill;

    // Caches the skill holder's original pivot before any zoom manipulation
    private void Awake()
    {
        originalPivot = skillHolder.pivot;
    }

    // Processes zoom and drag input each frame, applying movement only when the cursor is over the container and navigation is allowed
    private void Update()
    {
        if (skillHolder == null || skillTreeContainer == null)
        {
            Debug.LogError("skillHolder or skillTreeContainer is null!");
            return;
        }

        bool isMouseOverContainer = RectTransformUtility.RectangleContainsScreenPoint(skillTreeContainer, Input.mousePosition);

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

            float dpiMultiplier = PlayerPrefs.GetFloat("SkillTreeDragDPI", 1f);

            Vector2 moveDelta = new Vector2(delta.x * dpiMultiplier, delta.y * dpiMultiplier);

            Vector2 newPosition = skillHolder.anchoredPosition + moveDelta;
            skillHolder.anchoredPosition = ClampPosition(newPosition);
        }
    }

    // Moves the skill holder so the given skill's button is centered within the container
    public void CenterOnSkill(Skill skill)
    {
        LastSkill = skill;
        if (skill == null || skill.skillButton == null)
        {
            Debug.LogWarning("Skill or its button is not assigned!");
            return;
        }

        RectTransform skillRect = skill.skillButton.GetComponent<RectTransform>();
        if (skillRect == null)
        {
            Debug.LogWarning("RectTransform of the skill button not found!");
            return;
        }

        Vector3 worldPos = skillRect.position;

        Vector3 localInHolder3D = skillHolder.InverseTransformPoint(worldPos);
        Vector2 localInHolder = new Vector2(localInHolder3D.x, localInHolder3D.y);

        Vector2 containerCenter = new Vector2(
            skillTreeContainer.rect.width / 2f,
            -skillTreeContainer.rect.height / 2f
        );

        Vector2 offset = containerCenter - localInHolder * skillHolder.localScale.x;
        skillHolder.anchoredPosition = ClampPosition(offset);
    }

    // Returns the last skill that was centered on
    public Skill getLastSkill()
    {
        return LastSkill;
    }

    // Records the given skill as the last-navigated skill without moving the camera
    public void inputLastSkill(Skill skill)
    {
        LastSkill = skill;
    }

    // Scales the skill holder around the mouse cursor position, keeping the hovered point stationary
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

    // Clamps a candidate position so the skill holder never moves fully out of the container bounds
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

    // Resets the skill holder to its default position and zoom level
    public void ResetNavigation()
    {
        skillHolder.anchoredPosition = new Vector2(960f, -455f);
        skillHolder.localScale = new Vector3(0.8f, 0.8f, 1f);
    }

    // Stops any active drag when the component is disabled
    private void OnDisable()
    {
        isDragging = false;
        dragStartedInContainer = false;
    }
}
