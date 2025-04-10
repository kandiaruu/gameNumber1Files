using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class SkillInputHandler : MonoBehaviour, ISkillInputHandler
{
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; }
    [InjectAttribute1] private ISkillNotificationHandler notificationHandler { get; set; }
    [InjectAttribute1] private ITooltipManager tooltipManager { get; set; }
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1] private ISkillPanelManager skillPanelManager { get; set; }
    [InjectAttribute1] private IUIManager uiManager { get; set; }
    private Skill lastHoveredSkill;
    private bool wasDraggingLastFrame = false;
    private bool isMousePressed;
    private float pressStartTime;
    private float clickThreshold = 0.2f;
    private GameObject panelSelection;

    private void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        InitializeSkills();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMousePressed = true;
            pressStartTime = Time.unscaledTime;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isMousePressed = false;
        }

        if (skillTreeNavigation != null && !notificationHandler.skillNotificationPanelActive)
        {
            if (skillTreeNavigation.isDragging)
            {
                tooltipManager.HideTooltip();
            }
            else if (wasDraggingLastFrame && !skillTreeNavigation.isDragging)
            {
                CheckHoverAfterDrag();
            }
            else if (!skillTreeNavigation.isDragging)
            {
                tooltipManager.UpdatePosition(Input.mousePosition);
            }
            wasDraggingLastFrame = skillTreeNavigation.isDragging;
        }
    }

    private void InitializeSkills()
    {
        var skills = skillTreeManager.GetAllSkills();
        if (skills == null)
        {
            Debug.LogError("Skills array is null in SkillTreeManager!");
            return;
        }

        foreach (var skill in skills)
        {
            if (skill.skillButton != null)
            {
                var trigger = skill.skillButton.gameObject.GetComponent<EventTrigger>() ?? skill.skillButton.gameObject.AddComponent<EventTrigger>();
                AddEventTrigger(trigger, EventTriggerType.PointerEnter, () => OnPointerEnter(skill));
                AddEventTrigger(trigger, EventTriggerType.PointerExit, OnPointerExit);
                
                skill.skillButton.onClick.AddListener(() =>
                {
                    float pressDuration = Time.unscaledTime - pressStartTime;
                    if (pressDuration <= clickThreshold && !skill.isUnlocked && notificationHandler != null)
                    {
                        notificationHandler.ShowNotification(skill);
                    }
                    else if (notificationHandler == null)
                    {
                        Debug.LogError("NotificationHandler is null when clicking skill!");
                    }
                });
            }
        }
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action());
        trigger.triggers.Add(entry);
    }

    public void OnPointerEnter(Skill skill)
    {
        panelSelection = uiManager.GetPanel(UIManager.PanelType.Selection); 
        string currentGroup = skillPanelManager?.GetCurrentPanelName();
        if (string.IsNullOrEmpty(currentGroup))
        {
            Debug.LogWarning("Текущая группа не установлена в SkillTreeNavigation!");
            return;
        }

        if (skill.groupName != currentGroup)
        {
            Debug.Log($"Skill {skill.skillName} не принадлежит текущей группе {currentGroup}. Игнорируем наведение.");
            return;
        }

        // Проверяем, разрешён ли тултип для активной панели (включая дочерние)
        var activePanelConfig = uiManager.FindActivePanelConfig();
        if (activePanelConfig != null && !activePanelConfig.showTooltip)
        {
            Debug.Log($"Тултип отключён для панели {activePanelConfig.panelType}");
            return;
        }
        else {
            Debug.Log($"Тултип включён для панели {activePanelConfig?.panelType}");
        }

        if (!skill.questionIcon.activeSelf && !skillTreeNavigation.isDragging)
        {
            tooltipManager.ShowTooltip(skill, Input.mousePosition);
            lastHoveredSkill = skill;
        }
    }

    public void OnPointerExit()
    {
        tooltipManager.HideTooltip();
    }

    private void CheckHoverAfterDrag()
    {
        string currentGroup = skillTreeNavigation?.CurrentGroupName;
        if (string.IsNullOrEmpty(currentGroup))
        {
            Debug.LogWarning("Current group is not set in SkillTreeNavigation!");
            return;
        }

        var skills = skillTreeManager.GetAllSkillsInGroup(currentGroup);
        foreach (var skill in skills)
        {
            if (skill.skillButton != null && RectTransformUtility.RectangleContainsScreenPoint(skill.skillButton.GetComponent<RectTransform>(), Input.mousePosition))
            {
                lastHoveredSkill = skill;
                OnPointerEnter(skill);
                break;
            }
        }
    }
}