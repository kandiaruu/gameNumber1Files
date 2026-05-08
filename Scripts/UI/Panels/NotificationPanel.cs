//
// Generic notification panel for skill-related messages. Handles unlock prompts,
// reset confirmations, and upgrade-level reset via a slider. Dynamically spawns
// and clears action buttons for each use case.
//

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
    [SerializeField] private Slider levelSlider;
    [SerializeField] private TextMeshProUGUI sliderValueText;
    [SerializeField] private Vector2 fixedPosition = new Vector2(960f, -540f);

    private RectTransform notificationRect;
    private List<Button> dynamicButtons = new List<Button>();
    private Skill currentSkill;
    [InjectAttribute1] private ISkillTreeManager skillLogicManager { get; set; }
    [InjectAttribute1] private ISkillGuiManager skillGuiManager { get; set; }
    private bool isResetMode = false;

    // Injects dependencies, validates required serialized fields, and subscribes to slider changes
    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        base.Awake();
        notificationRect = GetComponent<RectTransform>();
        if (notificationRect == null) Debug.LogError("RectTransform for SkillNotificationPanel not found!");
        if (notificationText == null) Debug.LogError("notificationText is not assigned!");
        if (buttonContainer == null) Debug.LogError("buttonContainer is not assigned!");
        if (buttonPrefab == null) Debug.LogError("buttonPrefab is not assigned!");
        if (levelSlider == null) Debug.LogError("levelSlider is not assigned!");
        if (sliderValueText == null) Debug.LogError("sliderValueText is not assigned!");

        levelSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    // Opens the panel with a simple text message and a single Close button
    public void ShowMessage(string message)
    {
        Open();
        ClearButtons();

        if (levelSlider != null) levelSlider.gameObject.SetActive(false);
        if (sliderValueText != null) sliderValueText.gameObject.SetActive(false);

        notificationText.text = message;

        AddButton("Close", Close);
    }

    // Opens the panel with a context-sensitive unlock prompt for the given skill
    public void ShowNotification(Skill skill, ISkillTreeManager skillTreeManager = null)
    {
        Debug.Log("ShowNotification called for " + skill.skillName);
        currentSkill = skill;
        skillLogicManager = skillTreeManager ?? skillLogicManager;
        if (skillLogicManager == null)
        {
            Debug.LogError("skillLogicManager is not initialised!");
            return;
        }

        Open();
        ClearButtons();

        string groupName = GetGroupNameForSkill(skill);
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogError($"Could not find group for skill {skill.skillName}");
            Close();
            return;
        }

        if (!skill.hasQuestionState)
        {
            notificationText.text = $"Skill \"{skill.skillName}\" is hidden.\n" +
                                    $"Reveal cost: {skill.questionGoldCost} gold.";
            AddButton("Buy", () =>
            {
                skillLogicManager.BuyQuestionState(groupName, skill.skillIndex);
                Close();
            });
            AddButton("Cancel", Close);
        }
        else if (!skill.CanUnlock(skillLogicManager.GetAllSkillsInGroup(groupName)))
        {
            notificationText.text = $"Skill \"{skill.skillName}\" is locked.\n" +
                                    $"Prerequisites not met.";
            AddButton("Close", Close);
        }
        else if (skillLogicManager.GetSkillPoints() < skill.cost)
        {
            notificationText.text = $"Not enough skill points to unlock \"{skill.skillName}\".\n" +
                                    $"Required: {skill.cost} points.";
            AddButton("Close", Close);
        }
        else
        {
            notificationText.text = $"Unlock skill \"{skill.skillName}\"?\n" +
                                    $"Cost: {skill.cost} skill points.";
            AddButton("Unlock", () =>
            {
                skillLogicManager.UnlockSkill(groupName, skill.skillIndex);
                Close();
            });
            AddButton("Cancel", Close);
        }
    }

    // Opens a confirmation dialog for resetting a skill, noting how many dependent skills exist
    public void ShowResetConfirmation(Skill skill, int dependentCount, System.Action onConfirm)
    {
        Debug.Log($"ShowResetConfirmation called for {skill.skillName} with {dependentCount} dependent skills");
        currentSkill = skill;
        isResetMode = true;
        Open();
        ClearButtons();

        if (dependentCount > 0)
        {
            notificationText.text = $"Skill \"{skill.skillName}\" has {dependentCount} dependent skills.\n";
        }
        else
        {
            notificationText.text = $"Skill \"{skill.skillName}\" is at level {skill.currentLevel}.\n" +
                                    $"Are you sure you want to reset it?";
        }

        AddButton("Reset", () =>
        {
            onConfirm?.Invoke();
            isResetMode = false;
            Close();
        });
        AddButton("Cancel", () => { Close(); });
    }

    // Opens a slider-based dialog letting the player choose which upgrade level to reset to
    public void ShowUpgradeResetNotification(Skill skill, ISkillTreeManager skillTreeManager)
    {
        Debug.Log($"ShowUpgradeResetNotification called for {skill.skillName}");
        currentSkill = skill;
        skillLogicManager = skillTreeManager;
        isResetMode = true;
        Open();
        ClearButtons();

        levelSlider.gameObject.SetActive(true);
        sliderValueText.gameObject.SetActive(true);
        levelSlider.minValue = 1;
        levelSlider.maxValue = skill.currentLevel - 1;
        levelSlider.wholeNumbers = true;
        levelSlider.value = skill.currentLevel - 1;
        UpdateSliderText();

        notificationText.text = "Select the level to reset to:";

        AddButton("Reset", () =>
        {
            int targetLevel = (int)levelSlider.value;
            skillLogicManager.ResetSkillToLevel(currentSkill.groupName, currentSkill.skillIndex, targetLevel);
            skillGuiManager.UpdateUI();
            Close();
        });
        AddButton("Cancel", () => { Close(); });

        levelSlider.Select();
    }

    // Called when the slider value changes; updates the displayed target level label
    private void OnSliderValueChanged(float value)
    {
        UpdateSliderText();
    }

    // Refreshes the slider label to show the currently selected target level
    private void UpdateSliderText()
    {
        sliderValueText.text = $"Reset to level: {(int)levelSlider.value}";
    }

    // If in reset mode, re-opens the skill GUI panel on close; always clears buttons and hides the slider
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

    // Instantiates a button with the given label and click handler, then tracks it for later cleanup
    private Button AddButton(string buttonText, System.Action onClick)
    {
        var button = Instantiate(buttonPrefab, buttonContainer);
        button.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;
        button.onClick.AddListener(() => onClick?.Invoke());
        dynamicButtons.Add(button);
        return button;
    }

    // Removes all listeners from and destroys every dynamically created button
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

    // Searches all skill groups to find which group contains the given skill and returns its name
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
