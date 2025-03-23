using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;

public class SkillTreeManager : MonoBehaviour, ISkillTreeManager
{
    [SerializeField] private Skill[] skills;
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private Button skillsResetButton;
    [SerializeField] private int skillPoints = 3;
    private SkillTreeNavigation skillTreeNavigation;
    private NotificationManager notificationManager;
    private TooltipManager tooltipManager; // Добавлено

    [SerializeField] private int gold = 10;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button resetQuestionsButton;

    public bool skillNotificationPanelActive = false; // Флаг активности панели уведомлений

    private Skill lastHoveredSkill; // Последний навык, над которым был курсор
    private bool wasDraggingLastFrame = false; // Отслеживание состояния перетаскивания

    public ColorBlock[] originalColorBlocks;
        private void Awake()
    {
        DependencyContainer container = DependencyContainer.Instance;
        notificationManager = container.Resolve<NotificationManager>();
        if (notificationManager == null) Debug.LogError("NotificationManager не зарегистрирован в DependencyContainer!");

        tooltipManager = container.Resolve<TooltipManager>();
        if (tooltipManager == null) Debug.LogError("TooltipManager не зарегистрирован в DependencyContainer!");

        skillTreeNavigation = container.Resolve<SkillTreeNavigation>();
        if (skillTreeNavigation == null) Debug.LogError("skillTreeNavigation не зарегистрирован в DependencyContainer!");

        if (transform.parent != null) transform.SetParent(null);
        UnityEngine.Object.DontDestroyOnLoad(gameObject);

        InitializeSkills();
    }

    private void Start()
    {
        if (skillsResetButton == null) Debug.LogError("Кнопка сброса не назначена!");
        skillsResetButton.onClick.AddListener(ResetSkills);
        if (skillPointsText == null) Debug.LogError("Текст очков навыков не назначен!");
        UpdateSkillPointsUI();
        UpdateGoldUI();
        RefreshAllSkills();

        if (resetQuestionsButton == null) Debug.LogError("Кнопка сброса вопросов не назначена!");
        resetQuestionsButton.onClick.AddListener(ResetQuestionsAndGold);

        originalColorBlocks = new ColorBlock[skills.Length];
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i].skillButton != null)
            {
                originalColorBlocks[i] = skills[i].skillButton.colors;
            }
        }

        foreach (var skill in skills)
        {
            if (skill.skillButton != null)
            {
                var button = skill.skillButton.gameObject;
                var trigger = button.GetComponent<EventTrigger>();
                if (trigger == null) trigger = button.AddComponent<EventTrigger>();
                Debug.Log($"Добавлен EventTrigger для кнопки {skill.skillName}");

                EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener((data) => { OnPointerEnter(skill); });
                trigger.triggers.Add(enterEntry);

                EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener((data) => { OnPointerExit(); });
                trigger.triggers.Add(exitEntry);

                skill.skillButton.onClick.RemoveAllListeners();
                skill.skillButton.onClick.AddListener(() =>
                {
                    if (!skill.isUnlocked)
                    {
                        ShowNotification(skill);
                    }
                });
            }
        }
    }

    private void Update()
    {
        Debug.Log($"skillTreeNavigation = {(skillTreeNavigation != null ? skillTreeNavigation.ToString() : "null")}");
        Debug.Log($"skillTreeNavigation.isDragging = {(skillTreeNavigation != null ? skillTreeNavigation.isDragging.ToString() : "skillTreeNavigation is null")}");

        if (skillTreeNavigation != null && !skillNotificationPanelActive && tooltipManager != null)
        {
            if (skillTreeNavigation.isDragging)
            {
                tooltipManager.HideTooltip();
            }
            else if (wasDraggingLastFrame && !skillTreeNavigation.isDragging)
            {
                foreach (var skill in skills)
                {
                    if (skill.skillButton != null &&
                        RectTransformUtility.RectangleContainsScreenPoint(
                            skill.skillButton.GetComponent<RectTransform>(),
                            Input.mousePosition))
                    {
                        lastHoveredSkill = skill;
                        OnPointerEnter(skill);
                        break;
                    }
                }
            }
            else if (!skillTreeNavigation.isDragging)
            {
                Debug.Log("Обновление позиции тултипа...");
                tooltipManager.UpdatePosition(Input.mousePosition);
            }

            wasDraggingLastFrame = skillTreeNavigation.isDragging;
        }
    }

    public void OnPointerEnter(Skill skill)
    {
        Debug.Log($"OnPointerEnter вызван для {skill.skillName}");
        if (!skill.questionIcon.activeSelf &&
            (skillTreeNavigation == null || !skillTreeNavigation.isDragging) &&
            !skillNotificationPanelActive)
        {
            tooltipManager.ShowTooltip(skill, Input.mousePosition);
            lastHoveredSkill = skill;
        }
    }

    public void OnPointerExit()
    {
        Debug.Log("OnPointerExit вызван");
        tooltipManager.HideTooltip();
    }

    private void InitializeSkills()
    {
        var duplicateIndices = skills.GroupBy(s => s.skillIndex).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicateIndices.Any())
        {
            Debug.LogError($"Найдены дубликаты индексов навыков: {string.Join(", ", duplicateIndices)}");
        }

        foreach (var skill in skills)
        {
            skill.isUnlocked = false;

            skill.hasQuestionState = !skill.hasQuestionByDefault;
            if (skill.questionIcon != null)
            {
                skill.questionIcon.SetActive(!skill.hasQuestionState && !skill.isUnlocked);
            }
            else
            {
                Debug.LogError($"Иконка вопроса для {skill.skillName} не назначена!");
            }

            if (skill.lockIcon != null)
            {
                skill.originalPosition = skill.lockIcon.GetComponent<RectTransform>().anchoredPosition;
                skill.lockIconTransform = skill.lockIcon.GetComponent<RectTransform>();
                if (skill.lockIconTransform == null)
                {
                    Debug.LogError($"Transform для lockIcon {skill.lockIcon.name} у {skill.skillName} равен null!");
                }
                skill.lockIcon.SetActive(false);
            }
            else
            {
                Debug.LogError($"Иконка замка для {skill.skillName} не назначена!");
            }

            if (skill.skillButton == null)
            {
                Debug.LogError($"Кнопка для {skill.skillName} не назначена!");
            }
            else
            {
                skill.skillButton.onClick.RemoveAllListeners();
                skill.skillButton.onClick.AddListener(() =>
                {
                    if (!skill.isUnlocked)
                    {
                        ShowNotification(skill);
                    }
                });
            }
        }
    }

    public void UnlockSkill(int skillIndex)
    {
        Skill skill = skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null)
        {
            Debug.LogError($"Навык с индексом {skillIndex} не найден");
            return;
        }

        if (skill.isUnlocked) return;

        if (!skill.CanUnlock(skills) || skillPoints < skill.cost)
        {
            skill.ShakeLockIcon(this);
            return;
        }

        skillPoints -= skill.cost;
        skill.isUnlocked = true;
        UpdateSkillPointsUI();
        RefreshAllSkills();
    }

    private void ResetSkills()
    {
        int pointsToReturn = skills.Where(skill => skill.isUnlocked).Sum(skill => skill.cost);
        foreach (var skill in skills) skill.isUnlocked = false;
        skillPoints += pointsToReturn;
        UpdateSkillPointsUI();
        RefreshAllSkills();
    }

    private void UpdateSkillPointsUI()
    {
        skillPointsText.text = $"Очки навыков: {skillPoints}";
    }

    private void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"Золото: {gold}";
        else
            Debug.LogError("Текст золота не назначен!");
    }

    public void BuyQuestionState(int skillIndex)
    {
        Skill skill = skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null)
        {
            Debug.LogError($"Навык с индексом {skillIndex} не найден");
            return;
        }

        if (!skill.hasQuestionState)
        {
            if (gold < skill.questionGoldCost)
            {
                skill.ShakeLockIcon(this);
                return;
            }

            gold -= skill.questionGoldCost;
            skill.hasQuestionState = true;
            UpdateGoldUI();
            RefreshAllSkills();

            OnPointerEnter(skill);
        }
    }

    private void ResetQuestionsAndGold()
    {
        int goldToReturn = 0;
        foreach (var skill in skills)
        {
            if (!skill.isUnlocked && skill.hasQuestionState && skill.hasQuestionByDefault)
            {
                goldToReturn += skill.questionGoldCost;
                skill.hasQuestionState = false;
            }
        }
        gold += goldToReturn;
        UpdateGoldUI();
        RefreshAllSkills();
        Debug.Log($"Сброшены купленные вопросы для заблокированных навыков. Возвращено золота: {goldToReturn}");
    }

    private void RefreshAllSkills()
    {
        foreach (var skill in skills)
        {
            skill.UpdateUI(skillPoints >= skill.cost, skills);
        }
    }

    public void AddSkillPoints(int points)
    {
        skillPoints += points;
        UpdateSkillPointsUI();
        RefreshAllSkills();
    }

    private void ShowNotification(Skill skill)
    {
        if (notificationManager != null)
        {
            notificationManager.ShowNotification(skill);
            skillNotificationPanelActive = true;
        }
        else
        {
            Debug.LogError("NotificationManager не инициализирован!");
        }
    }

    public void OnNotificationPanelClosed()
    {
        skillNotificationPanelActive = false;
        Debug.Log("SkillNotificationPanel закрыта, skillNotificationPanelActive = false");
    }

    public void EnableSkillButtons(bool enable)
    {
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i].skillButton != null)
            {
                Button button = skills[i].skillButton;
                if (enable)
                {
                    if (i < originalColorBlocks.Length && originalColorBlocks[i] != null)
                    {
                        button.colors = originalColorBlocks[i];
                    }
                    button.interactable = true;
                }
                else
                {
                    if (i >= originalColorBlocks.Length || originalColorBlocks[i] == null)
                    {
                        originalColorBlocks[i] = button.colors;
                    }
                    ColorBlock tempColorBlock = button.colors;
                    tempColorBlock.disabledColor = tempColorBlock.normalColor;
                    tempColorBlock.colorMultiplier = 1f;
                    button.colors = tempColorBlock;
                    button.interactable = false;
                }
            }
        }
    }

    public Skill[] GetAllSkills()
    {
        return skills;
    }

    public int GetSkillPoints()
    {
        return skillPoints;
    }
}