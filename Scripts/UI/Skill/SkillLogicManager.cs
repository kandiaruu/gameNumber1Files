using UnityEngine;
using System.Linq;

public class SkillLogicManager : MonoBehaviour, ISkillTreeManager
{
    [SerializeField] private Skill[] skills;
    [SerializeField] private int skillPoints = 3;
    [SerializeField] private int gold = 10;
    [InjectAttribute1]
    private ISkillUIManager SkillUIManager { get; set; }

    public event System.Action<int> OnSkillPointsChanged;
    public event System.Action<int> OnGoldChanged;
    public event System.Action OnSkillsUpdated;

    void Awake()
    {
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    // Убираем InjectDependencies из Start, перенесём в GameBootstrap
    void Start()
    {
        foreach (var skill in skills)
        {
            skill.Initialize();
        }
        SkillUIManager.RefreshAllSkills();
    }

    public void UnlockSkill(int skillIndex)
    {
        Skill skill = skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null || skill.isUnlocked || !skill.CanUnlock(skills) || skillPoints < skill.cost)
        {
            if (skill != null) skill.ShakeLockIcon(this);
            return;
        }

        skillPoints -= skill.cost;
        skill.isUnlocked = true;
        OnSkillPointsChanged?.Invoke(skillPoints);
        OnSkillsUpdated?.Invoke();
    }

    public void BuyQuestionState(int skillIndex)
    {
        Skill skill = skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null || skill.hasQuestionState || gold < skill.questionGoldCost)
        {
            if (skill != null) skill.ShakeLockIcon(this);
            return;
        }

        gold -= skill.questionGoldCost;
        skill.hasQuestionState = true;
        OnGoldChanged?.Invoke(gold);
        OnSkillsUpdated?.Invoke();
    }

    public void ResetSkills()
    {
        int pointsToReturn = skills.Where(s => s.isUnlocked).Sum(s => s.cost);
        foreach (var skill in skills) skill.isUnlocked = false;
        skillPoints += pointsToReturn;
        OnSkillPointsChanged?.Invoke(skillPoints);
        OnSkillsUpdated?.Invoke();
    }

    public void ResetQuestionsAndGold()
    {
        int goldToReturn = 0;
        foreach (var skill in skills)
        {
            if (!skill.isUnlocked && skill.hasQuestionState)
            {
                if (skill.hasQuestionByDefault)
                {
                    goldToReturn += skill.questionGoldCost;
                }
                skill.hasQuestionState = !skill.hasQuestionByDefault;
            }
        }
        gold += goldToReturn;
        OnGoldChanged?.Invoke(gold);
        OnSkillsUpdated?.Invoke();
    }

    public void AddSkillPoints(int points)
    {
        skillPoints += points;
        OnSkillPointsChanged?.Invoke(skillPoints);
    }

    public int GetSkillPoints() => skillPoints;
    public Skill[] GetAllSkills() => skills;
    public int GetGold() => gold;
    public void EnableSkillButtons(bool enable) { }
    public void OnNotificationPanelClosed() { }
}