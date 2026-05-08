//
// Displays detailed information about a selected skill and provides buttons to
// unlock, upgrade, reset, or reset upgrades for that skill.
//

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillGuiPanel : BasePanel, ISkillGuiPanel
{
    [SerializeField] private TextMeshProUGUI skillInfoText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button resetUpgradesButton;
    [InjectAttribute1] private ISkillNotificationPanel notificationPanel { get; set; }
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; }

    private Skill currentSkill;

    // Injects dependencies and wires up all button listeners
    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);

        if (skillInfoText == null) Debug.LogError("skillInfoText is not assigned!");
        if (closeButton == null) Debug.LogError("closeButton is not assigned!");
        if (resetButton == null) Debug.LogError("resetButton is not assigned!");
        if (upgradeButton == null) Debug.LogError("upgradeButton is not assigned!");
        if (resetUpgradesButton == null) Debug.LogError("resetUpgradesButton is not assigned!");

        closeButton.onClick.AddListener(Close);
        resetButton.onClick.AddListener(ResetSkill);
        upgradeButton.onClick.AddListener(UpgradeSkill);
        resetUpgradesButton.onClick.AddListener(OpenResetNotification);
    }

    // Stores the target skill, opens the panel, and refreshes displayed info
    public void ShowSkillGui(Skill skill)
    {
        Debug.Log($"ShowSkillGui called for {skill.skillName}");
        currentSkill = skill;

        Open();
        UpdateUI();
    }

    // Refreshes skill name, level, button labels, and button visibility based on current skill state
    public void UpdateUI()
    {
        if (currentSkill == null) return;

        skillInfoText.text = $"Skill: {currentSkill.skillName}\n" +
                             $"Current level: {currentSkill.currentLevel}";

        resetButton.gameObject.SetActive(currentSkill.isUnlocked && currentSkill.canBeReset);

        TextMeshProUGUI upgradeButtonText = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (currentSkill.maxUpgrades == 0 || currentSkill.currentLevel >= currentSkill.maxUpgrades)
        {
            upgradeButtonText.text = "MAX LEVEL";
            upgradeButton.interactable = false;
        }
        else
        {
            upgradeButtonText.text = "UPGRADE";
            upgradeButton.interactable = currentSkill.CanUpgrade(skillTreeManager.GetGold());
        }
        upgradeButton.gameObject.SetActive(currentSkill.isUnlocked);

        resetUpgradesButton.gameObject.SetActive(currentSkill.isUnlocked && currentSkill.currentLevel > 1);
    }

    // Resets the current skill through the skill tree manager and closes the panel
    private void ResetSkill()
    {
        if (currentSkill != null && skillTreeManager != null)
        {
            skillTreeManager.ResetSkill(currentSkill.groupName, currentSkill.skillIndex);
            Close();
        }
    }

    // Upgrades the current skill by one level and refreshes the UI
    private void UpgradeSkill()
    {
        if (currentSkill != null && skillTreeManager != null)
        {
            skillTreeManager.UpgradeSkill(currentSkill.groupName, currentSkill.skillIndex);
            UpdateUI();
        }
    }

    // Opens the upgrade-reset notification panel for the current skill and closes this panel
    private void OpenResetNotification()
    {
        if (currentSkill != null && notificationPanel != null)
        {
            notificationPanel.ShowUpgradeResetNotification(currentSkill, skillTreeManager);
            Close();
        }
    }

    // Clears the current skill reference and resets the info text when the panel closes
    protected override void OnClose()
    {
        currentSkill = null;
        skillInfoText.text = "";
    }
}
