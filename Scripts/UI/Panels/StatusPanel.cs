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

    void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        resetButton.onClick.AddListener(OnResetButtonPressed);
        RefreshUI();
    }

    void OnResetButtonPressed()
    {
        PlayerStats.ResetAttributes();
        RefreshUI();
    }

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

    public override void Open()
    {
        base.Open();
        RefreshUI();
    }
}