using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

public class SkillNotificationPanel : BasePanel, ISkillNotificationPanel
{
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Slider levelSlider; // Добавляем ползунок
    [SerializeField] private TextMeshProUGUI sliderValueText; // Текст для отображения значения ползунка
    [SerializeField] private Vector2 fixedPosition = new Vector2(960f, -540f);

    private RectTransform notificationRect;
    private List<Button> dynamicButtons = new List<Button>();
    private Skill currentSkill;
    [InjectAttribute1]
    private ISkillTreeManager skillLogicManager { get; set; }
    [InjectAttribute1]
    private ISkillGuiManager skillGuiManager { get; set; }
    private bool isResetMode = false;

    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        base.Awake();
        notificationRect = GetComponent<RectTransform>();
        if (notificationRect == null) Debug.LogError("RectTransform для SkillNotificationPanel не найден!");
        if (notificationText == null) Debug.LogError("notificationText не назначен!");
        if (buttonContainer == null) Debug.LogError("buttonContainer не назначен!");
        if (buttonPrefab == null) Debug.LogError("buttonPrefab не назначен!");
        if (levelSlider == null) Debug.LogError("levelSlider не назначен!");
        if (sliderValueText == null) Debug.LogError("sliderValueText не назначен!");

        levelSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    public void ShowNotification(Skill skill, ISkillTreeManager skillTreeManager = null)
    {
        // Существующий код без изменений
        Debug.Log("ShowNotification вызван для " + skill.skillName);
        currentSkill = skill;
        skillLogicManager = skillTreeManager ?? skillLogicManager;
        if (skillLogicManager == null)
        {
            Debug.LogError("skillLogicManager не инициализирован!");
            return;
        }

        Open();
        ClearButtons();

        string groupName = GetGroupNameForSkill(skill);
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogError($"Не удалось найти группу для навыка {skill.skillName}");
            Close();
            return;
        }

        if (!skill.hasQuestionState)
        {
            notificationText.text = $"Навык \"{skill.skillName}\" скрыт.\n" +
                                   $"Стоимость раскрытия: {skill.questionGoldCost} золота.";
            AddButton("Купить", () => 
            { 
                skillLogicManager.BuyQuestionState(groupName, skill.skillIndex); 
                Close(); 
            });
            AddButton("Отмена", Close);
        }
        else if (!skill.CanUnlock(skillLogicManager.GetAllSkillsInGroup(groupName)))
        {
            notificationText.text = $"Навык \"{skill.skillName}\" заблокирован.\n" +
                                   $"Не выполнены обязательные требования.";
            AddButton("Закрыть", Close);
        }
        else if (skillLogicManager.GetSkillPoints() < skill.cost)
        {
            notificationText.text = $"Недостаточно очков навыков для разблокировки \"{skill.skillName}\".\n" +
                                   $"Требуется: {skill.cost} очков.";
            AddButton("Закрыть", Close);
        }
        else
        {
            notificationText.text = $"Разблокировать навык \"{skill.skillName}\"?\n" +
                                   $"Стоимость: {skill.cost} очков навыков.";
            AddButton("Разблокировать", () => 
            { 
                skillLogicManager.UnlockSkill(groupName, skill.skillIndex); 
                Close(); 
            });
            AddButton("Отмена", Close);
        }
    }

    public void ShowResetConfirmation(Skill skill, int dependentCount, System.Action onConfirm)
    {
        // Существующий код без изменений
        Debug.Log($"ShowResetConfirmation вызван для {skill.skillName} с {dependentCount} зависимыми навыками");
        currentSkill = skill;
        isResetMode = true;
        Open();
        ClearButtons();
    
        if (dependentCount > 0) {
        notificationText.text = $"Навык \"{skill.skillName}\" имеет {dependentCount} зависимых навыков.\n";
        }
        else {
        notificationText.text = $"У Навыка \"{skill.skillName}\" {skill.currentLevel} уровень.\n" +
                                $"Вы уверены, что хотите сбросить его?";
        }

        var resetButton = AddButton("Сбросить", () =>
        {
            onConfirm?.Invoke();
            isResetMode = false;
            Close();
        });
        AddButton("Отмена", () => {
            Close();
        });
    }

    public void ShowUpgradeResetNotification(Skill skill, ISkillTreeManager skillTreeManager)
    {
        Debug.Log($"ShowUpgradeResetNotification вызван для {skill.skillName}");
        currentSkill = skill;
        skillLogicManager = skillTreeManager;
        isResetMode = true;
        Open();
        ClearButtons();

        // Настраиваем ползунок
        levelSlider.gameObject.SetActive(true);
        sliderValueText.gameObject.SetActive(true);
        levelSlider.minValue = 1;
        levelSlider.maxValue = skill.currentLevel - 1; // До какого уровня можно сбросить
        levelSlider.wholeNumbers = true;
        levelSlider.value = skill.currentLevel - 1; // Начальное значение
        UpdateSliderText();

        notificationText.text = $"В1";

        AddButton("Сбросить", () =>
        {
            int targetLevel = (int)levelSlider.value;
            skillLogicManager.ResetSkillToLevel(currentSkill.groupName, currentSkill.skillIndex, targetLevel);
            skillGuiManager.UpdateUI(); // Обновляем UI после сброса
            Close();
        });
        AddButton("Отмена", () =>
        {
            Close();
        });

        levelSlider.Select(); // Фокус на ползунке
    }

    private void OnSliderValueChanged(float value)
    {
        UpdateSliderText();
    }

    private void UpdateSliderText()
    {
        sliderValueText.text = $"Сбросить до уровня: {(int)levelSlider.value}";
    }

    protected override void OnClose()
    {
        if (isResetMode && currentSkill != null)
        {
            skillGuiManager?.ShowSkillGui(currentSkill);
        }
        ClearButtons();
        levelSlider.gameObject.SetActive(false);
        sliderValueText.gameObject.SetActive(false);
        currentSkill = null;
        isResetMode = false;
    }

    private Button AddButton(string buttonText, System.Action onClick)
    {
        var button = Instantiate(buttonPrefab, buttonContainer);
        button.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;
        button.onClick.AddListener(() => onClick?.Invoke());
        dynamicButtons.Add(button);
        return button;
    }

    private void ClearButtons()
    {
        foreach (var button in dynamicButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                UnityEngine.Object.Destroy(button.gameObject);
            }
        }
        dynamicButtons.Clear();
    }

    private string GetGroupNameForSkill(Skill skill)
    {
        var skillGroups = skillLogicManager.GetSkillGroups();
        if (skillGroups == null)
        {
            Debug.LogError("skillGroups is null in GetGroupNameForSkill!");
            return string.Empty;
        }

        foreach (var group in skillGroups)
        {
            if (group.skills.Contains(skill))
                return group.groupName;
        }
        Debug.LogWarning($"Skill {skill.skillName} not found in any group!");
        return string.Empty;
    }
}