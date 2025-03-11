using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;

public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }

    [SerializeField] private Skill[] skills;
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private Button resetButton;
    [SerializeField] private int skillPoints = 3;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;

    private bool isTooltipActive = false;
    private RectTransform tooltipRect;
    [SerializeField] private float TOOLTIP_OFFSET_X = 500f; // Фиксированный отступ справа от курсора

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        InitializeSkills();
    }

    private void Start()
    {
        if (resetButton == null) Debug.LogError("Кнопка сброса не назначена!");
        resetButton.onClick.AddListener(ResetSkills);
        if (skillPointsText == null) Debug.LogError("Текст очков навыков не назначен!");
        UpdateSkillPointsUI();
        RefreshAllSkills();

        if (tooltipPanel != null)
        {
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect == null)
            {
                Debug.LogError("RectTransform для tooltipPanel не найден!");
            }
            else
            {
                Debug.Log("tooltipRect успешно инициализирован");
                tooltipPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("tooltipPanel не назначен в инспекторе!");
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
            }
        }
    }

    private void Update()
    {
        if (isTooltipActive && tooltipPanel != null && tooltipRect != null)
        {
            Vector3 mousePosition = Input.mousePosition;
            Vector3 targetPosition = new Vector3(mousePosition.x + TOOLTIP_OFFSET_X, mousePosition.y, 0f); // Фиксированный отступ справа

            Vector2 tooltipSize = tooltipRect.sizeDelta;

            // Корректировка, если тултип выходит за правую границу
            if (targetPosition.x + tooltipSize.x > Screen.width)
            {
                targetPosition.x = Screen.width - tooltipSize.x; // Прижимаем к правому краю
            }
            // Корректировка по Y
            targetPosition.y = Mathf.Clamp(targetPosition.y, tooltipSize.y, Screen.height);

            // Плавное перемещение
            tooltipRect.position = Vector3.Lerp(tooltipRect.position, targetPosition, Time.unscaledDeltaTime * 15f);
        }
    }

    public void OnPointerEnter(Skill skill)
    {
        Debug.Log("OnPointerEnter вызван для " + skill.skillName);
        if (tooltipPanel != null && tooltipText != null && !isTooltipActive)
        {
            isTooltipActive = true;
            tooltipPanel.SetActive(true);
            Canvas.ForceUpdateCanvases();

            string tooltipContent = $"Навык: {skill.skillName}\n" +
                                   $"Описание: {skill.description}\n" +
                                   $"Характеристики: {string.Join(", ", skill.characteristics ?? new string[] { "Нет данных" })}\n" +
                                   $"Макс. улучшений: {skill.maxUpgrades}";
            tooltipText.text = tooltipContent;

            // Начальная позиция справа от курсора
            Vector3 mousePosition = Input.mousePosition;
            Vector3 initialPosition = new Vector3(mousePosition.x + TOOLTIP_OFFSET_X, mousePosition.y, 0f);

            Vector2 tooltipSize = tooltipRect.sizeDelta;
            if (initialPosition.x + tooltipSize.x > Screen.width)
            {
                initialPosition.x = Screen.width - tooltipSize.x; // Прижимаем к правому краю, если не помещается
            }
            initialPosition.y = Mathf.Clamp(initialPosition.y, tooltipSize.y, Screen.height);

            tooltipRect.position = initialPosition;
        }
    }

    public void OnPointerExit()
    {
        Debug.Log("OnPointerExit вызван");
        if (tooltipPanel != null && isTooltipActive)
        {
            isTooltipActive = false;
            tooltipPanel.SetActive(false);
        }
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

            if (skill.lockIcon != null)
            {
                skill.originalPosition = skill.lockIcon.GetComponent<RectTransform>().anchoredPosition;
                skill.lockIconTransform = skill.lockIcon.GetComponent<RectTransform>();
                if (skill.lockIconTransform == null)
                {
                    Debug.LogError($"Transform для lockIcon {skill.lockIcon.name} у {skill.skillName} равен null!");
                }
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
                skill.skillButton.onClick.AddListener(() => UnlockSkill(skill.skillIndex));
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
}