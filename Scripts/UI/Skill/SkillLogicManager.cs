using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class SkillLogicManager : MonoBehaviour, ISkillTreeManager
{
    [SerializeField] private SkillGroup[] skillGroups;
    [SerializeField] private int skillPoints = 3;
    [SerializeField] private int gold = 10;
    [InjectAttribute1] private ISkillUIManager SkillUIManager { get; set; }
    [InjectAttribute1] private ISkillPanelManager SkillPanelManager { get; set; }
    [InjectAttribute1] private ISkillPanelUI skillPanelUI { get; set; }
    [InjectAttribute1] private INotificationManager notificationManager { get; set; }

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
            group.Initialize();
            foreach (var skill in group.skills)
            {
                skill.HandleVisibilityChange(true, group.skills);
            }
        }

        SkillUIManager?.RefreshAllSkills();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SetSkillsVisibility("Hidden", 0, true);
        }
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

    public void ResetSkills()
    {
        string currentGroupName = SkillPanelManager?.GetCurrentPanelName() ?? "Normal";
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == currentGroupName);
        if (group == null)
        {
            Debug.LogWarning($"Группа {currentGroupName} не найдена для сброса навыков!");
            return;
        }

        int pointsToReturn = group.skills
            .Where(s => s.isUnlocked && s.canBeReset)
            .Sum(s => s.cost);

        foreach (var skill in group.skills)
        {
            if (skill.isUnlocked && skill.canBeReset)
            {
                skill.isUnlocked = false;
                skill.UpdateUI(true, GetAllSkillsInGroup(currentGroupName));
            }
        }

        skillPoints += pointsToReturn;
        OnSkillPointsChanged?.Invoke(skillPoints);
        OnSkillsUpdated?.Invoke();

        Debug.Log($"Сброшено навыков: {group.skills.Count(s => s.isUnlocked == false && s.canBeReset)}. Возвращено {pointsToReturn} очков.");
    }

    public void ResetQuestionsAndGold()
    {
        string currentGroupName = SkillPanelManager?.GetCurrentPanelName() ?? "Normal";
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == currentGroupName);
        if (group == null)
        {
            Debug.LogWarning($"Группа {currentGroupName} не найдена для сброса вопросов!");
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

    public void SetSkillVisibility(string groupName, int skillIndex, bool isVisible)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null) return;

        Skill skill = group.skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null) return;

        skill.HandleVisibilityChange(isVisible, GetAllSkillsInGroup(groupName));
        OnSkillsUpdated?.Invoke();
    }

    public void SetSkillsVisibility(string groupName, int skillIndex, bool isVisible)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null) return;

        foreach (var skill in group.skills)
        {
            skill.HandleVisibilityChange(isVisible, GetAllSkillsInGroup(groupName));
        }
        skillPanelUI?.UnlockPanel(groupName);
        OnSkillsUpdated?.Invoke();
    }

    public void ResetSkill(string groupName, int skillIndex)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null)
        {
            Debug.LogWarning($"Группа {groupName} не найдена!");
            return;
        }

        Skill skillToReset = group.skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skillToReset == null || !skillToReset.isUnlocked)
        {
            Debug.LogWarning($"Навык с индексом {skillIndex} в группе {groupName} не найден или уже сброшен!");
            return;
        }

        if (!skillToReset.canBeReset)
        {
            Debug.LogWarning($"Навык {skillToReset.skillName} нельзя сбросить!");
            return;
        }

        List<Skill> dependentSkills = new List<Skill>();
        int totalPointsToReturn = skillToReset.cost;
        FindDependentSkills(group, skillToReset, dependentSkills, ref totalPointsToReturn);

        if (dependentSkills.Count > 0)
        {
            notificationManager?.ShowNotification(skillToReset, dependentSkills.Count, () =>
            {
                dependentSkills.Add(skillToReset);
                foreach (var skill in dependentSkills)
                {
                    skill.isUnlocked = false;
                    skill.UpdateUI(true, GetAllSkillsInGroup(groupName));
                }
                skillPoints += totalPointsToReturn;
                OnSkillPointsChanged?.Invoke(skillPoints);
                OnSkillsUpdated?.Invoke();
                Debug.Log($"Сброшены навыки: {string.Join(", ", dependentSkills.Select(s => s.skillName))}. Возвращено {totalPointsToReturn} очков.");
            });
        }
        else
        {
            skillToReset.isUnlocked = false;
            skillToReset.UpdateUI(true, GetAllSkillsInGroup(groupName));
            skillPoints += skillToReset.cost;
            OnSkillPointsChanged?.Invoke(skillPoints);
            OnSkillsUpdated?.Invoke();
            Debug.Log($"Сброшен навык: {skillToReset.skillName}. Возвращено {skillToReset.cost} очков.");
        }
    }

    private void FindDependentSkills(SkillGroup group, Skill skillToReset, List<Skill> skillsToReset, ref int totalPointsToReturn)
    {
        foreach (var skill in group.skills)
        {
            if (!skill.isUnlocked || skillsToReset.Contains(skill)) continue;

            if (skill.prerequisiteIndices.Contains(skillToReset.skillIndex))
            {
                skillsToReset.Add(skill);
                totalPointsToReturn += skill.cost;
                FindDependentSkills(group, skill, skillsToReset, ref totalPointsToReturn);
            }
        }
    }

    public void UpgradeSkill(string groupName, int skillIndex)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null)
        {
            Debug.LogWarning($"Группа {groupName} не найдена!");
            return;
        }

        Skill skill = group.skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null || !skill.isUnlocked)
        {
            Debug.LogWarning($"Навык с индексом {skillIndex} в группе {groupName} не найден или не разблокирован!");
            return;
        }

        if (!skill.CanUpgrade(gold))
        {
            Debug.LogWarning($"Недостаточно золота или навык {skill.skillName} уже на максимальном уровне!");
            skill.ShakeLockIcon(this);
            return;
        }

        gold -= skill.upgradeCosts[skill.currentLevel];
        skill.currentLevel++;
        OnGoldChanged?.Invoke(gold);
        OnSkillsUpdated?.Invoke();
        Debug.Log($"Навык {skill.skillName} улучшен до уровня {skill.currentLevel}. Потрачено {skill.upgradeCosts[skill.currentLevel - 1]} золота.");
    }
}