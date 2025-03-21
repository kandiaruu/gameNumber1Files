using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class SkillNotificationPanel : BasePanel
{
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Vector2 fixedPosition = new Vector2(960f, -540f);

    private RectTransform notificationRect;
    private List<Button> dynamicButtons = new List<Button>();
    private Skill currentSkill;
    [SerializeField] private SkillTreeManager skillTreeManager;

    protected override void Awake()
    {
        base.Awake();
        notificationRect = GetComponent<RectTransform>();
        if (notificationRect == null)
        {
            Debug.LogError("RectTransform для SkillNotificationPanel не найден!");
        }
        if (notificationText == null)
        {
            Debug.LogError("notificationText не назначен в инспекторе!");
        }
        if (buttonContainer == null)
        {
            Debug.LogError("buttonContainer не назначен в инспекторе!");
        }
        if (buttonPrefab == null)
        {
            Debug.LogError("buttonPrefab не назначен в инспекторе!");
        }
    }

    public override void Open()
    {
        base.Open();
        notificationRect.anchoredPosition = fixedPosition;
        Debug.Log("Уведомление открыто.");
    }

    protected override void OnClose()
    {
        ClearButtons();
        if (skillTreeManager != null)
        {
            if (skillTreeManager.originalColorBlocks != null)
            {
                skillTreeManager.EnableSkillButtons(true);
                Debug.Log("Кнопки навыков включены.");
            }
            else
            {
                Debug.LogWarning("originalColorBlocks не установлен в SkillTreeManager!");
            }
        }
        else
        {
            Debug.LogWarning("SkillTreeManager не установлен в SkillNotificationPanel!");
        }
        currentSkill = null;
    }

    public void ShowNotification(Skill skill, SkillTreeManager skillTreeManager)
    {
        this.skillTreeManager = skillTreeManager;
        currentSkill = skill;
        Open();
        Debug.Log("Уведомление открыто.1");

        ClearButtons();

        if (!skill.hasQuestionState)
        {
            Debug.Log($"Условие: !skill.hasQuestionState. hasQuestionState = {skill.hasQuestionState}");
            notificationText.text = $"Навык \"{skill.skillName}\" скрыт.\n" +
                                   $"Стоимость раскрытия: {skill.questionGoldCost} золота.";
            AddButton("Купить", () =>
            {
                skillTreeManager.BuyQuestionState(skill.skillIndex);
                Close();
            });
            AddButton("Отмена", Close);
        }
        else if (!skill.CanUnlock(skillTreeManager.GetAllSkills()))
        {
            Debug.Log($"Условие: !skill.CanUnlock. CanUnlock = {skill.CanUnlock(skillTreeManager.GetAllSkills())}");
            notificationText.text = $"Навык \"{skill.skillName}\" заблокирован.\n" +
                                   $"Не выполнены обязательные требования.";
            AddButton("Закрыть", Close);
        }
        else if (skillTreeManager.GetSkillPoints() < skill.cost)
        {
            Debug.Log($"Условие: skillTreeManager.GetSkillPoints() < skill.cost. SkillPoints = {skillTreeManager.GetSkillPoints()}, Cost = {skill.cost}");
            notificationText.text = $"Недостаточно очков навыков для разблокировки \"{skill.skillName}\".\n" +
                                   $"Требуется: {skill.cost} очков.";
            AddButton("Закрыть", Close);
        }
        else
        {
            Debug.Log("Условие: всё готово для разблокировки");
            notificationText.text = $"Разблокировать навык \"{skill.skillName}\"?\n" +
                                   $"Стоимость: {skill.cost} очков навыков.";
            AddButton("Разблокировать", () =>
            {
                skillTreeManager.UnlockSkill(skill.skillIndex);
                Close();
            });
            AddButton("Отмена", Close);
        }
    }

    private void AddButton(string buttonText, Action onClick)
    {
        if (buttonPrefab == null)
        {
            Debug.LogError("buttonPrefab не назначен! Кнопки не могут быть созданы.");
            return;
        }
        if (buttonContainer == null)
        {
            Debug.LogError("buttonContainer не назначен! Кнопки не могут быть созданы.");
            return;
        }

        Button newButton = Instantiate(buttonPrefab, buttonContainer);
        var textComponent = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogError($"TextMeshProUGUI не найден на кнопке {newButton.name}!");
        }
        else
        {
            textComponent.text = buttonText;
        }
        newButton.onClick.AddListener(() => onClick?.Invoke());
        dynamicButtons.Add(newButton);
        Debug.Log($"Кнопка '{buttonText}' создана и добавлена в {buttonContainer.name}");
    }

    private void ClearButtons()
    {
        Debug.Log("Очистка кнопок в SkillNotificationPanel");
        foreach (var button in dynamicButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }
        }
        dynamicButtons.Clear();
    }
}