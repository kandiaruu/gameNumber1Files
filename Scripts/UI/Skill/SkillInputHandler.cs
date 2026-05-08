//
// Handles player input for skill interactions including hovering, clicking, and tooltip management
//

using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections;

public class SkillInputHandler : MonoBehaviour, ISkillInputHandler
{
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; }
    [InjectAttribute1] private ISkillNotificationHandler notificationHandler { get; set; }
    [InjectAttribute1] private ITooltipManager tooltipManager { get; set; }
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1] private ISkillPanelManager skillPanelManager { get; set; }
    [InjectAttribute1] private IUIManager uiManager { get; set; }
    [InjectAttribute1] private ISkillGuiManager skillGuiManager { get; set; }
    private Skill lastHoveredSkill;
    private bool wasDraggingLastFrame = false;
    private float pressStartTime;
    private float clickThreshold = 0.2f;
    private Coroutine hoverCoroutine;
    private GameObject panelSelection;

    //
    // Initializes dependencies and sets up skill input handlers
    //
    private void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        InitializeSkills();
    }

    //
    // Processes mouse input for dragging and tooltip visibility
    //
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pressStartTime = Time.unscaledTime;
        }

        if (skillTreeNavigation != null && !notificationHandler.skillNotificationPanelActive)
        {
            if (skillTreeNavigation.isDragging)
            {
                Debug.Log("Dragging detected, hiding tooltip.");
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
        if (Input.GetKeyDown(KeyCode.S))
        {
            skillPanelManager.toggleSelection();
        }
    }

    //
    // Sets up event triggers and click handlers for all skills
    //
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

                skill.skillButton.onClick.RemoveAllListeners();
                skill.skillButton.onClick.AddListener(() =>
                {
                    float pressDuration = Time.unscaledTime - pressStartTime;
                    if (pressDuration <= clickThreshold)
                    {   
                        if (skillPanelManager.GetCurrentPanelName() == "Hidden")
                        {
                            skillTreeNavigation.inputLastSkill(skill);
                        }
                        if (skill.isUnlocked && skillGuiManager != null)
                        {
                            skillGuiManager.ShowSkillGui(skill);
                        }
                        else if (!skill.isUnlocked && notificationHandler != null)
                        {
                            notificationHandler.ShowNotification(skill);
                        }
                    }
                });
            }
        }
    }

    //
    // Adds an event trigger callback to an event trigger component
    //
    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action());
        trigger.triggers.Add(entry);
    }

    //
    // Handles pointer enter event with delayed tooltip display
    //
    public void OnPointerEnter(Skill skill)
    {
        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);
        
        hoverCoroutine = StartCoroutine(DelayedTooltip(skill));
    }
    
    //
    // Displays tooltip after a delay if conditions are met
    //
    private IEnumerator DelayedTooltip(Skill skill)
    {
        yield return new WaitForSecondsRealtime(0.05f);

        panelSelection = uiManager.GetPanel(UIManager.PanelType.Selection); 
        string currentGroup = skillPanelManager?.GetCurrentPanelName();
        if (string.IsNullOrEmpty(currentGroup))
        {
            Debug.LogWarning("Текущая группа не установлена в SkillTreeNavigation!");
            yield break;
        }

        if (skill.groupName != currentGroup)
        {
            Debug.Log($"Skill {skill.skillName} не принадлежит текущей группе {currentGroup}. Игнорируем наведение.");
            yield break;
        }

        var activePanelConfig = uiManager.FindActivePanelConfig();
        if (activePanelConfig != null && !activePanelConfig.showTooltip)
        {
            Debug.Log($"Тултип отключён для панели {activePanelConfig.panelType}");
            yield break;
        }

        if (!skill.questionIcon.activeSelf && !skillTreeNavigation.isDragging && skill.isVisible)
        {
            tooltipManager.ShowTooltip(skill, Input.mousePosition);
            lastHoveredSkill = skill;
        }
    }

    //
    // Handles pointer exit event and hides the tooltip
    //
    public void OnPointerExit()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        tooltipManager.HideTooltip();
    }

    //
    // Checks if any skill is under the cursor after dragging stops
    //
    private void CheckHoverAfterDrag()
    {
        string currentGroup = skillPanelManager?.GetCurrentPanelName();;
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
