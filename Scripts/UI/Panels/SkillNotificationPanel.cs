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
    private ISkillTreeManager currentManager;

    // Изменение: добавляем override для переопределения Awake из BasePanel
    public override void Awake()
    {
        base.Awake(); // Вызываем базовую реализацию
        notificationRect = GetComponent<RectTransform>();
        if (notificationRect == null) Debug.LogError("RectTransform для SkillNotificationPanel не найден!");
        if (notificationText == null) Debug.LogError("notificationText не назначен!");
        if (buttonContainer == null) Debug.LogError("buttonContainer не назначен!");
        if (buttonPrefab == null) Debug.LogError("buttonPrefab не назначен!");

        currentManager = DependencyContainer.Instance.Resolve<ISkillTreeManager>();
        if (currentManager == null) Debug.LogError("ISkillTreeManager не зарегистрирован!");
    }

    public void ShowNotification(Skill skill, ISkillTreeManager skillTreeManager = null)
    {
        Debug.Log("ShowNotification вызван для " + skill.skillName);
        currentSkill = skill;
        currentManager = skillTreeManager ?? currentManager;
        Open();
        ClearButtons();

        if (!skill.hasQuestionState)
        {
            notificationText.text = $"Навык \"{skill.skillName}\" скрыт.\n" +
                                   $"Стоимость раскрытия: {skill.questionGoldCost} золота.";
            AddButton("Купить", () => { currentManager.BuyQuestionState(skill.skillIndex); Close(); });
            AddButton("Отмена", Close);
        }
        else if (!skill.CanUnlock(currentManager.GetAllSkills()))
        {
            notificationText.text = $"Навык \"{skill.skillName}\" заблокирован.\n" +
                                   $"Не выполнены обязательные требования.";
            AddButton("Закрыть", Close);
        }
        else if (currentManager.GetSkillPoints() < skill.cost)
        {
            notificationText.text = $"Недостаточно очков навыков для разблокировки \"{skill.skillName}\".\n" +
                                   $"Требуется: {skill.cost} очков.";
            AddButton("Закрыть", Close);
        }
        else
        {
            notificationText.text = $"Разблокировать навык \"{skill.skillName}\"?\n" +
                                   $"Стоимость: {skill.cost} очков навыков.";
            AddButton("Разблокировать", () => { currentManager.UnlockSkill(skill.skillIndex); Close(); });
            AddButton("Отмена", Close);
        }

        currentManager.EnableSkillButtons(false);
    }

    protected override void OnClose()
    {
        ClearButtons();
        if (currentManager != null)
        {
            currentManager.EnableSkillButtons(true);
            currentManager.OnNotificationPanelClosed();
        }
        currentSkill = null;
        currentManager = null;
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
}