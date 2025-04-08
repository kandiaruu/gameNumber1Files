using UnityEngine;
using System.Linq;

public class SkillLogicManager : MonoBehaviour, ISkillTreeManager
{
    [SerializeField] private SkillGroup[] skillGroups;
    [SerializeField] private int skillPoints = 3;
    [SerializeField] private int gold = 10;
    [InjectAttribute1] private ISkillUIManager SkillUIManager { get; set; }

    public event System.Action<int> OnSkillPointsChanged;
    public event System.Action<int> OnGoldChanged;
    public event System.Action OnSkillsUpdated;

    void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        foreach (var group in skillGroups)
        {
            foreach (var skill in group.skills)
            {
                skill.Initialize();
            }
        }
        SkillUIManager?.RefreshAllSkills();
    }

    public void UnlockSkill(string groupName, int skillIndex)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null) return;

        Skill skill = group.skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null || skill.isUnlocked || !skill.CanUnlock(GetAllSkillsInGroup(groupName)) || skillPoints < skill.cost)
        {
            if (skill != null) skill.ShakeLockIcon(this);
            return;
        }

        skillPoints -= skill.cost;
        skill.isUnlocked = true;
        OnSkillPointsChanged?.Invoke(skillPoints);
        OnSkillsUpdated?.Invoke();
    }

    public void BuyQuestionState(string groupName, int skillIndex)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null) return;

        Skill skill = group.skills.FirstOrDefault(s => s.skillIndex == skillIndex);
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

    public void ResetSkills(string groupName)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null)
        {
            Debug.LogWarning($"Группа {groupName} не найдена для сброса навыков!");
            return;
        }

        int pointsToReturn = group.skills.Where(s => s.isUnlocked).Sum(s => s.cost);
        foreach (var skill in group.skills)
        {
            skill.isUnlocked = false;
        }
        skillPoints += pointsToReturn;
        OnSkillPointsChanged?.Invoke(skillPoints);
        OnSkillsUpdated?.Invoke();
    }

    public void ResetQuestionsAndGold(string groupName)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null)
        {
            Debug.LogWarning($"Группа {groupName} не найдена для сброса вопросов!");
            return;
        }

        int goldToReturn = 0;
        foreach (var skill in group.skills)
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

    public void ResetSkills() // Оставляем для совместимости, но теперь он не используется напрямую
    {
        int pointsToReturn = 0;
        foreach (var group in skillGroups)
        {
            pointsToReturn += group.skills.Where(s => s.isUnlocked).Sum(s => s.cost);
            foreach (var skill in group.skills)
            {
                skill.isUnlocked = false;
            }
        }
        skillPoints += pointsToReturn;
        OnSkillPointsChanged?.Invoke(skillPoints);
        OnSkillsUpdated?.Invoke();
    }

    public void ResetQuestionsAndGold() // Оставляем для совместимости
    {
        int goldToReturn = 0;
        foreach (var group in skillGroups)
        {
            foreach (var skill in group.skills)
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
    public Skill[] GetAllSkills() => skillGroups.SelectMany(g => g.skills).ToArray();
    public Skill[] GetAllSkillsInGroup(string groupName) => 
        skillGroups.FirstOrDefault(g => g.groupName == groupName)?.skills ?? new Skill[0];
    public int GetGold() => gold;
    public SkillGroup[] GetSkillGroups() => skillGroups;
    public void EnableSkillButtons(bool enable) { }
    public void OnNotificationPanelClosed() { }
}