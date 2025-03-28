using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SkillUIManager : MonoBehaviour, ISkillUIManager
{
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button skillsResetButton;
    [SerializeField] private Button resetQuestionsButton;
    [InjectAttribute1]
    private ISkillTreeManager SkillLogicManager { get; set; }
    private ColorBlock[] originalColorBlocks;

    void Awake()
    {
        // InjectDependencies теперь вызывается в GameBootstrap.Awake
        ValidateUIElements();
        InitializeUI(); // Переносим сюда
    }

    void Start()
    {
        if (SkillLogicManager == null)
        {
            throw new System.NullReferenceException("SkillLogicManager is not injected!");
        }

        skillsResetButton.onClick.AddListener(SkillLogicManager.ResetSkills);
        resetQuestionsButton.onClick.AddListener(SkillLogicManager.ResetQuestionsAndGold);

        SkillLogicManager.OnSkillPointsChanged += points => skillPointsText.text = $"Очки навыков: {points}";
        SkillLogicManager.OnGoldChanged += gold => goldText.text = $"Золото: {gold}";
        SkillLogicManager.OnSkillsUpdated += RefreshAllSkills;
    }

    public void EnsureDependencies()
    {
        if (SkillLogicManager == null)
        {
            throw new System.NullReferenceException("SkillLogicManager is not injected!");
        }
        else
        {
            Debug.Log("SkillLogicManager is injected!");
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
        var skills = SkillLogicManager.GetAllSkills();
        if (skills == null)
        {
            Debug.LogError("GetAllSkills returned null!");
            return;
        }

        originalColorBlocks = new ColorBlock[skills.Length];
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i].skillButton != null)
            {
                originalColorBlocks[i] = skills[i].skillButton.colors;
            }
            else
            {
                Debug.LogWarning($"skills[{i}].skillButton is null in SkillUIManager!");
            }
        }
        skillPointsText.text = $"Очки навыков: {SkillLogicManager.GetSkillPoints()}";
        goldText.text = $"Золото: {SkillLogicManager.GetGold()}";
        RefreshAllSkills();
    }

    public void RefreshAllSkills()
    {
        if (SkillLogicManager == null) return;

        var skills = SkillLogicManager.GetAllSkills();
        if (skills == null) return;

        foreach (var skill in skills)
        {
            skill.UpdateUI(SkillLogicManager.GetSkillPoints() >= skill.cost, skills);
        }
    }

    public void EnableSkillButtons(bool enable)
    {
        if (SkillLogicManager == null)
        {
            Debug.LogError("SkillLogicManager is null in EnableSkillButtons!");
            return;
        }
        if (originalColorBlocks == null)
        {
            Debug.LogError("originalColorBlocks is not initialized!");
            return;
        }

        var skills = SkillLogicManager.GetAllSkills();
        if (skills == null)
        {
            Debug.LogError("skills array is null!");
            return;
        }

        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i].skillButton != null)
            {
                Button button = skills[i].skillButton;
                if (enable)
                {
                    if (i < originalColorBlocks.Length && originalColorBlocks[i] != null)
                    {
                        button.colors = originalColorBlocks[i];
                    }
                    button.interactable = true;
                }
                else
                {
                    if (i < originalColorBlocks.Length)
                    {
                        if (originalColorBlocks[i] == null)
                        {
                            originalColorBlocks[i] = button.colors;
                        }
                        ColorBlock tempColorBlock = button.colors;
                        tempColorBlock.disabledColor = tempColorBlock.normalColor;
                        tempColorBlock.colorMultiplier = 1f;
                        button.colors = tempColorBlock;
                        button.interactable = false;
                    }
                }
            }
        }
    }
}