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
    [SerializeField] private Vector2 fixedPosition = new Vector2(960f, -540f);

    private RectTransform notificationRect;
    private List<Button> dynamicButtons = new List<Button>();
    private Skill currentSkill;
    [InjectAttribute1]
    private ISkillTreeManager skillLogicManager { get; set; }
    [InjectAttribute1]
    private ISkillUIManager skillUIManager { get; set; }

    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        base.Awake();
        notificationRect = GetComponent<RectTransform>();
        if (notificationRect == null) Debug.LogError("RectTransform для SkillNotificationPanel не найден!");
        if (notificationText == null) Debug.LogError("notificationText не назначен!");
        if (buttonContainer == null) Debug.LogError("buttonContainer не назначен!");
        if (buttonPrefab == null) Debug.LogError("buttonPrefab не назначен!");
    }

    public void ShowNotification(Skill skill, ISkillTreeManager skillTreeManager = null)
    {
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

        skillUIManager?.EnableSkillButtons(false);
    }

    protected override void OnClose()
    {
        ClearButtons();
        if (skillUIManager != null)
        {
            skillUIManager.EnableSkillButtons(true);
        }
        currentSkill = null;
        // Не обнуляем skillLogicManager, если он инжектируется
    }

    private void AddButton(string buttonText, Action onClick)
    {
        if (buttonPrefab == null || buttonContainer == null) return;

        Button newButton = UnityEngine.Object.Instantiate(buttonPrefab, buttonContainer);
        var textComponent = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null) textComponent.text = buttonText;
        newButton.onClick.AddListener(() => onClick?.Invoke());
        dynamicButtons.Add(newButton);
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