using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class SkillUIManager : MonoBehaviour, ISkillUIManager
{
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button skillsResetButton;
    [SerializeField] private Button resetQuestionsButton;
    [InjectAttribute1] private ISkillTreeManager SkillLogicManager { get; set; }
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1] private ISkillPanelManager skillPanelManager { get; set; }
    [InjectAttribute1] private IUIManager uiManager { get; set; }
    private Dictionary<string, ColorBlock[]> originalColorBlocks;
    private bool lastButtonState = true; // Для отслеживания изменений состояния

    void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        ValidateUIElements();
        InitializeUI();
    }

    void Start()
    {
        if (SkillLogicManager == null) throw new System.NullReferenceException("SkillLogicManager is not injected!");
        if (skillTreeNavigation == null) throw new System.NullReferenceException("SkillTreeNavigation is not injected!");
        if (skillPanelManager == null) throw new System.NullReferenceException("SkillPanelManager is not injected!");

        skillsResetButton.onClick.AddListener(() => 
        {
            SkillLogicManager.ResetSkills();
        });
        resetQuestionsButton.onClick.AddListener(() => 
        {
            SkillLogicManager.ResetQuestionsAndGold();
        });

        SkillLogicManager.OnSkillPointsChanged += points => skillPointsText.text = $"Очки навыков: {points}";
        SkillLogicManager.OnGoldChanged += gold => goldText.text = $"Золото: {gold}";
        SkillLogicManager.OnSkillsUpdated += RefreshAllSkills;

        DisableButtonColorChange();
        UpdateButtonState(); // Проверка состояния при старте
    }

    void Update()
    {
        // Проверяем, разрешено ли взаимодействие с кнопками
        bool allowInteraction = uiManager.ShouldAllowSkillButtonInteraction();

        // Обновляем состояние кнопок только если оно изменилось
        if (allowInteraction != lastButtonState)
        {
            EnableSkillButtons(allowInteraction);
            lastButtonState = allowInteraction;
        }
    }

    private void ValidateUIElements()
    {
        if (skillPointsText == null) throw new System.NullReferenceException("SkillPointsText is not assigned!");
        if (goldText == null) throw new System.NullReferenceException("GoldText is not assigned!");
        if (skillsResetButton == null) throw new System.NullReferenceException("SkillsResetButton is not assigned!");
        if (resetQuestionsButton == null) throw new System.NullReferenceException("ResetQuestionsButton is not assigned!");
    }

    private void InitializeUI()
    {
        var allSkills = SkillLogicManager?.GetAllSkills();
        if (allSkills == null)
        {
            Debug.LogError("GetAllSkills returned null!");
            return;
        }

        var skillGroups = SkillLogicManager?.GetSkillGroups();
        if (skillGroups == null)
        {
            Debug.LogError("GetSkillGroups returned null!");
            return;
        }

        originalColorBlocks = new Dictionary<string, ColorBlock[]>();
        foreach (var group in skillGroups)
        {
            var skills = SkillLogicManager.GetAllSkillsInGroup(group.groupName);
            originalColorBlocks[group.groupName] = new ColorBlock[skills.Length];
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i].skillButton != null)
                {
                    originalColorBlocks[group.groupName][i] = skills[i].skillButton.colors;
                }
            }
        }

        skillPointsText.text = $"Очки навыков: {SkillLogicManager.GetSkillPoints()}";
        goldText.text = $"Золото: {SkillLogicManager.GetGold()}";
        RefreshAllSkills();
    }

    private void DisableButtonColorChange()
    {
        var allSkills = SkillLogicManager?.GetAllSkills();
        if (allSkills == null) return;

        foreach (var skill in allSkills)
        {
            if (skill.skillButton != null)
            {
                ColorBlock colors = skill.skillButton.colors;
                colors.highlightedColor = colors.normalColor;
                colors.pressedColor = colors.normalColor;
                colors.selectedColor = colors.normalColor;
                colors.colorMultiplier = 1f;
                skill.skillButton.colors = colors;
            }
        }
    }

    private void UpdateButtonState()
    {
        bool allowInteraction = uiManager.ShouldAllowSkillButtonInteraction();
        EnableSkillButtons(allowInteraction);
        lastButtonState = allowInteraction;
    }

    public void RefreshAllSkills()
    {
        if (SkillLogicManager == null) return;

        var allSkills = SkillLogicManager.GetAllSkills();
        if (allSkills == null) return;

        foreach (var skill in allSkills)
        {
            skill.UpdateUI(SkillLogicManager.GetSkillPoints() >= skill.cost, 
                SkillLogicManager.GetAllSkillsInGroup(GetGroupNameForSkill(skill)));
        }
    }

    private string GetGroupNameForSkill(Skill skill)
    {
        var skillGroups = SkillLogicManager.GetSkillGroups();
        foreach (var group in skillGroups)
        {
            if (group.skills.Contains(skill))
                return group.groupName;
        }
        Debug.LogWarning($"Skill {skill.skillName} not found in any group!");
        return string.Empty;
    }

    public void EnableSkillButtons(bool enable)
    {
        if (SkillLogicManager == null || originalColorBlocks == null) return;

        var allSkills = SkillLogicManager.GetAllSkills();
        if (allSkills == null) return;

        var skillGroups = SkillLogicManager.GetSkillGroups();
        if (skillGroups == null) return;

        foreach (var group in skillGroups)
        {
            var skills = SkillLogicManager.GetAllSkillsInGroup(group.groupName);
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i].skillButton != null)
                {
                    Button button = skills[i].skillButton;
                    if (enable)
                    {
                        if (originalColorBlocks.ContainsKey(group.groupName) && 
                            i < originalColorBlocks[group.groupName].Length)
                        {
                            ColorBlock colors = originalColorBlocks[group.groupName][i];
                            colors.highlightedColor = colors.normalColor;
                            colors.pressedColor = colors.normalColor;
                            colors.selectedColor = colors.normalColor;
                            button.colors = colors;
                        }
                        button.interactable = true;
                    }
                    else
                    {
                        if (originalColorBlocks.ContainsKey(group.groupName))
                        {
                            if (originalColorBlocks[group.groupName][i] == null)
                            {
                                originalColorBlocks[group.groupName][i] = button.colors;
                            }
                            ColorBlock tempColorBlock = button.colors;
                            tempColorBlock.disabledColor = tempColorBlock.normalColor;
                            tempColorBlock.highlightedColor = tempColorBlock.normalColor;
                            tempColorBlock.pressedColor = tempColorBlock.normalColor;
                            tempColorBlock.selectedColor = tempColorBlock.normalColor;
                            tempColorBlock.colorMultiplier = 1f;
                            button.colors = tempColorBlock;
                            button.interactable = false;
                        }
                    }
                }
            }
        }
    }
}