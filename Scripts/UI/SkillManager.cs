using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }

    [SerializeField] private Skill[] skills;
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private Button resetButton;
    [SerializeField] private int skillPoints = 3;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeSkills();
    }

    private void Start()
    {
        if (resetButton == null) Debug.LogError("Reset Button is not assigned!");
        resetButton.onClick.AddListener(ResetSkills);
        if (skillPointsText == null) Debug.LogError("Skill Points Text is not assigned!");
        UpdateSkillPointsUI();
        RefreshAllSkills();
    }

    private void InitializeSkills()
    {
        // Проверка на уникальность индексов
        var duplicateIndices = skills.GroupBy(s => s.skillIndex).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicateIndices.Any())
        {
            Debug.LogError($"Duplicate skill indices found: {string.Join(", ", duplicateIndices)}");
        }

        foreach (var skill in skills)
        {
            skill.isUnlocked = false;

            if (skill.lockIcon != null)
            {
                skill.originalPosition = skill.lockIcon.GetComponent<RectTransform>().anchoredPosition;
                skill.lockIconTransform = skill.lockIcon.GetComponent<RectTransform>();
                if (skill.lockIconTransform == null)
                {
                    Debug.LogError($"Transform for lockIcon {skill.lockIcon.name} of {skill.skillName} is null! Object active: {skill.lockIcon.activeSelf}");
                }
                else
                {
                    Debug.Log($"Initialized {skill.skillName} with lockIconTransform at {skill.originalPosition}");
                }
            }
            else
            {
                Debug.LogError($"Lock Icon for {skill.skillName} is not assigned!");
            }

            if (skill.skillButton == null)
            {
                Debug.LogError($"Skill Button for {skill.skillName} is not assigned!");
            }
            else
            {
                skill.skillButton.onClick.RemoveAllListeners();
                skill.skillButton.onClick.AddListener(() => UnlockSkill(skill.skillIndex));
            }
        }
    }

    public void UnlockSkill(int skillIndex)
    {
        Skill skill = skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null)
        {
            Debug.LogError($"No skill found with index {skillIndex}");
            return;
        }

        Debug.Log($"Trying to unlock {skill.skillName}, skillPoints={skillPoints}, cost={skill.cost}, isUnlocked={skill.isUnlocked}");
        if (skill.isUnlocked) return;

        if (!skill.CanUnlock(skills))
        {
            Debug.Log($"Prerequisites not met for {skill.skillName}");
            skill.ShakeLockIcon(this);
            return;
        }
        if (skillPoints < skill.cost)
        {
            Debug.Log($"Not enough skill points for {skill.skillName}");
            skill.ShakeLockIcon(this);
            return;
        }

        skillPoints -= skill.cost;
        skill.isUnlocked = true;
        Debug.Log($"Unlocked {skill.skillName}, remaining points: {skillPoints}");
        UpdateSkillPointsUI();
        RefreshAllSkills();
    }

    private void ResetSkills()
    {
        int pointsToReturn = skills.Where(skill => skill.isUnlocked).Sum(skill => skill.cost);

        foreach (var skill in skills)
        {
            skill.isUnlocked = false;
        }

        skillPoints += pointsToReturn;
        UpdateSkillPointsUI();
        RefreshAllSkills();
    }

    private void UpdateSkillPointsUI()
    {
        skillPointsText.text = $"Skill Points: {skillPoints}";
    }

    private void RefreshAllSkills()
    {
        foreach (var skill in skills)
        {
            skill.UpdateUI(skillPoints >= skill.cost, skills);
        }
    }

    public void AddSkillPoints(int points)
    {
        skillPoints += points;
        UpdateSkillPointsUI();
        RefreshAllSkills();
    }
}