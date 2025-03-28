using UnityEngine;
using UnityEngine.EventSystems;

public class SkillInputHandler : MonoBehaviour, ISkillInputHandler
{
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; }
    [InjectAttribute1] private ISkillNotificationHandler notificationHandler { get; set; }
    [InjectAttribute1] private ITooltipManager tooltipManager { get; set; }
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    private Skill lastHoveredSkill;
    private bool wasDraggingLastFrame = false;

    private void Start()
    {
        DependencyContainer1.InjectDependencies(this); // Добавляем инъекцию
        InitializeSkills();
    }

    private void Update()
    {
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
                    if (!skill.isUnlocked && notificationHandler != null)
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
        if (!skill.questionIcon.activeSelf && !skillTreeNavigation.isDragging && !notificationHandler.skillNotificationPanelActive)
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
        var skills = skillTreeManager.GetAllSkills();
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