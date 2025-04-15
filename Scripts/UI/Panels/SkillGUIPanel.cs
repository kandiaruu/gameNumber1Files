using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillGuiPanel : BasePanel, ISkillGuiPanel
{
    [SerializeField] private TextMeshProUGUI skillInfoText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button upgradeButton; // Кнопка "Улучшить"
    [SerializeField] private Button resetUpgradesButton; // Новая кнопка "Сбросить улучшения"
    [InjectAttribute1] private ISkillNotificationPanel notificationPanel { get; set; }

    [InjectAttribute1]
    private ISkillTreeManager skillTreeManager { get; set; }

    private Skill currentSkill;

    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);

        if (skillInfoText == null) Debug.LogError("skillInfoText не назначен!");
        if (closeButton == null) Debug.LogError("closeButton не назначен!");
        if (resetButton == null) Debug.LogError("resetButton не назначен!");
        if (upgradeButton == null) Debug.LogError("upgradeButton не назначен!");
        if (resetUpgradesButton == null) Debug.LogError("resetUpgradesButton не назначен!");

        closeButton.onClick.AddListener(Close);
        resetButton.onClick.AddListener(ResetSkill);
        upgradeButton.onClick.AddListener(UpgradeSkill);
        resetUpgradesButton.onClick.AddListener(OpenResetNotification); // Привязываем метод к кнопке
    }

    public void ShowSkillGui(Skill skill)
    {
        Debug.Log($"ShowSkillGui вызван для {skill.skillName}");
        currentSkill = skill;

        Open();
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (currentSkill == null) return;

        skillInfoText.text = $"Навык: {currentSkill.skillName}\n" +
                             $"Текущий уровень: {currentSkill.currentLevel}";

        resetButton.gameObject.SetActive(currentSkill.isUnlocked && currentSkill.canBeReset);

        TextMeshProUGUI upgradeButtonText = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (currentSkill.maxUpgrades == 0 || currentSkill.currentLevel >= currentSkill.maxUpgrades)
        {
            upgradeButtonText.text = "МАКС. УРОВЕНЬ";
            upgradeButton.interactable = false;
        }
        else
        {
            upgradeButtonText.text = $"УЛУЧШИТЬ";
            upgradeButton.interactable = currentSkill.CanUpgrade(skillTreeManager.GetGold());
        }
        upgradeButton.gameObject.SetActive(currentSkill.isUnlocked);

        // Обновляем состояние кнопки "Сбросить улучшения"
        resetUpgradesButton.gameObject.SetActive(currentSkill.isUnlocked && currentSkill.currentLevel > 1);
    }

    private void ResetSkill()
    {
        if (currentSkill != null && skillTreeManager != null)
        {
            skillTreeManager.ResetSkill(currentSkill.groupName, currentSkill.skillIndex);
            Close();
        }
    }

    private void UpgradeSkill()
    {
        if (currentSkill != null && skillTreeManager != null)
        {
            skillTreeManager.UpgradeSkill(currentSkill.groupName, currentSkill.skillIndex);
            UpdateUI(); // Обновляем UI после улучшения
        }
    }

    private void OpenResetNotification()
    {
        if (currentSkill != null && notificationPanel != null)
        {
            notificationPanel.ShowUpgradeResetNotification(currentSkill, skillTreeManager); // Открываем панель с ползунком
            Close();
        }
    }

    protected override void OnClose()
    {
        currentSkill = null;
        skillInfoText.text = "";
    }
}