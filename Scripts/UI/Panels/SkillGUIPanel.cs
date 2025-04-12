using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillGuiPanel : BasePanel, ISkillGuiPanel
{
    [SerializeField] private TextMeshProUGUI skillInfoText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button upgradeButton; // Новая кнопка "Улучшить"

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

        closeButton.onClick.AddListener(Close);
        resetButton.onClick.AddListener(ResetSkill);
        upgradeButton.onClick.AddListener(UpgradeSkill);
    }

    public void ShowSkillGui(Skill skill)
    {
        Debug.Log($"ShowSkillGui вызван для {skill.skillName}");
        currentSkill = skill;

        Open();
        UpdateUI();
    }

    private void UpdateUI()
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

    protected override void OnClose()
    {
        currentSkill = null;
        skillInfoText.text = "";
    }
}