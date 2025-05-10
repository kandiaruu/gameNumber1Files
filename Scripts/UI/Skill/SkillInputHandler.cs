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

    private void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        InitializeSkills();
    }

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
            skillPanelManager.toggleSelection(); // Переключаем панель выбора
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

                skill.skillButton.onClick.RemoveAllListeners(); // Очищаем старые слушатели
                skill.skillButton.onClick.AddListener(() =>
                {
                    float pressDuration = Time.unscaledTime - pressStartTime;
                    if (pressDuration <= clickThreshold) // Проверяем, что это клик, а не удержание
                    {   
                        if (skillPanelManager.GetCurrentPanelName() == "Hidden")
                        {
                            skillTreeNavigation.inputLastSkill(skill); // Передаем навык в менеджер панели
                        }
                        if (skill.isUnlocked && skillGuiManager != null)
                        {
                            skillGuiManager.ShowSkillGui(skill); // Показываем SkillGui для разблокированного навыка
                        }
                        else if (!skill.isUnlocked && notificationHandler != null)
                        {
                            notificationHandler.ShowNotification(skill); // Существующая логика для заблокированных навыков
                        }
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
        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);
        
        hoverCoroutine = StartCoroutine(DelayedTooltip(skill));
    }
    
    private IEnumerator DelayedTooltip(Skill skill)
    {
        yield return new WaitForSecondsRealtime(0.05f); // задержка в 0.2 секунды

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

    public void OnPointerExit()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        tooltipManager.HideTooltip();
    }


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