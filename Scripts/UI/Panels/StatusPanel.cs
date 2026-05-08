//
// Displays and manages the player status panel, including level, experience,
// gold, skill/attribute points, HP/MP/fatigue, and per-attribute upgrade buttons.
//

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class StatusPanel : BasePanel
{
    [InjectAttribute1] public IPlayerStats PlayerStats { get; set; }
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI experienceText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI skillPointsText;
    public TextMeshProUGUI attributePointsText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;
    public TextMeshProUGUI fatigueText;
    public Button resetButton;

    public Transform attributesButtonsParent;
    public AttributeButton attributeButtonPrefab;

    Dictionary<string, string> attrDisplayNames = new()
    {
        {"Strength", "STR"},
        {"Agility", "DEX"},
        {"Intelligence", "INT"},
        {"Endurance", "END"},
        {"Perception", "PER"},
        {"Resistance", "FOR"},
        {"Luck", "LCK"},
    };

    List<AttributeButton> attributeButtons = new();

    // Injects dependencies, wires up the reset button, and performs the initial UI refresh
    void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        resetButton.onClick.AddListener(OnResetButtonPressed);
        RefreshUI();
    }

    // Resets all attribute points via PlayerStats and refreshes the UI
    void OnResetButtonPressed()
    {
        PlayerStats.ResetAttributes();
        RefreshUI();
    }

    // Destroys existing attribute buttons and spawns a fresh set from the display-name dictionary
    void CreateAttributeButtons()
    {
        foreach (Transform child in attributesButtonsParent)
            Destroy(child.gameObject);
        attributeButtons.Clear();

        foreach (var kv in attrDisplayNames)
        {
            var button = Instantiate(attributeButtonPrefab, attributesButtonsParent);
            attributeButtons.Add(button);
            button.button.onClick.AddListener(() => button.OnClick());
        }
    }

    // Returns the current numeric value of the given attribute key from PlayerStats
    int GetStatValue(string attrKey)
    {
        return attrKey switch
        {
            "Strength" => PlayerStats.Strength,
            "Agility" => PlayerStats.Agility,
            "Intelligence" => PlayerStats.Intelligence,
            "Endurance" => PlayerStats.Endurance,
            "Perception" => PlayerStats.Perception,
            "Resistance" => PlayerStats.Resistance,
            "Luck" => PlayerStats.Luck,
            _ => 0
        };
    }

    // Updates all stat labels and re-initialises every attribute button with current values
    public void RefreshUI()
    {
        if (PlayerStats == null) return;

        levelText.text = $"Level: {PlayerStats.Level}";
        experienceText.text = $"Exp: {PlayerStats.Experience}/{PlayerStats.ExperienceToNextLevel}";
        goldText.text = $"Gold: {PlayerStats.Gold}";
        skillPointsText.text = $"SkillPoints: {PlayerStats.SkillPoints}";
        attributePointsText.text = $"AttributePoints: {PlayerStats.AttributePoints}";
        hpText.text = $"HP: {PlayerStats.CurrentHP}/{PlayerStats.MaxHP}, {PlayerStats.HpRegen}";
        mpText.text = $"MP: {PlayerStats.CurrentMP}/{PlayerStats.MaxMP}, {PlayerStats.MpRegen}";
        fatigueText.text = $"Fatigue: {PlayerStats.Fatigue:0}%";

        if (attributeButtons.Count == 0)
            CreateAttributeButtons();

        bool canUsePoints = PlayerStats.AttributePoints > 0;
        for (int i = 0; i < attributeButtons.Count; ++i)
        {
            var attrKey = attributeButtons[i].name;
            var dictKey = attrDisplayNames.Keys.ElementAt(i);
            attributeButtons[i].Init(dictKey, attrDisplayNames[dictKey], GetStatValue(dictKey), OnAttributeIncreasePressed, canUsePoints);
        }
    }

    // Spends one attribute point to increase the specified attribute by 1, then refreshes the UI
    void OnAttributeIncreasePressed(string attrKey)
    {
        if (PlayerStats.AttributePoints <= 0)
            return;

        switch (attrKey)
        {
            case "Strength": PlayerStats.Strength += 1; break;
            case "Agility": PlayerStats.Agility += 1; break;
            case "Intelligence": PlayerStats.Intelligence += 1; break;
            case "Endurance": PlayerStats.Endurance += 1; break;
            case "Perception": PlayerStats.Perception += 1; break;
            case "Resistance": PlayerStats.Resistance += 1; break;
            case "Luck": PlayerStats.Luck += 1; break;
        }
        PlayerStats.AttributePoints -= 1;
        RefreshUI();
    }

    // Opens the panel and immediately refreshes all displayed stats
    public override void Open()
    {
        base.Open();
        RefreshUI();
    }
}
