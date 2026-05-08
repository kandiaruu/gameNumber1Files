//
// Core logic manager for the skill tree system, handling skill unlocking, upgrades, and point management
//

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class SkillLogicManager : MonoBehaviour, ISkillTreeManager
{
    [SerializeField] private SkillGroup[] skillGroups;

    [InjectAttribute1] private ISkillUIManager SkillUIManager { get; set; }
    [InjectAttribute1] private ISkillPanelManager SkillPanelManager { get; set; }
    [InjectAttribute1] private ISkillPanelUI skillPanelUI { get; set; }
    [InjectAttribute1] private INotificationManager notificationManager { get; set; }
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    [InjectAttribute1] private IPlayerStats playerStats { get; set; }

    public event System.Action<int> OnSkillPointsChanged;
    public event System.Action<int> OnGoldChanged;
    public event System.Action OnSkillsUpdated;

    //
    // Initializes dependencies and ensures manager persistence
    //
    void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    //
    // Initializes all skill groups and refreshes the skill UI
    //
    void Start()
    {
        foreach (var group in skillGroups)
        {
            group.Initialize();
        }
        SkillUIManager?.RefreshAllSkills();
    }

    //
    // Unlocks a skill if prerequisites are met and player has sufficient skill points
    //
    public void UnlockSkill(string groupName, int skillIndex)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null) return;

        Skill skill = group.skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null || skill.isUnlocked || !skill.CanUnlock(GetAllSkillsInGroup(groupName)) || playerStats.SkillPoints < skill.cost)
        {
            if (skill != null) skill.ShakeLockIcon(this);
            return;
        }

        playerStats.SkillPoints -= skill.cost;
        skill.isUnlocked = true;
        OnSkillPointsChanged?.Invoke(playerStats.SkillPoints);
        OnSkillsUpdated?.Invoke();
    }

    //
    // Purchases question state for a skill if player has sufficient gold
    //
    public void BuyQuestionState(string groupName, int skillIndex)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null) return;

        Skill skill = group.skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null || skill.hasQuestionState || playerStats.Gold < skill.questionGoldCost)
        {
            if (skill != null) skill.ShakeLockIcon(this);
            return;
        }

        playerStats.Gold -= skill.questionGoldCost;
        skill.hasQuestionState = true;
        OnGoldChanged?.Invoke(playerStats.Gold);
        OnSkillsUpdated?.Invoke();
    }

    //
    // Resets all resettable skills in the current panel and returns skill points
    //
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
                ResetSkillUpgrades(currentGroupName, skill.skillIndex);
                skill.isUnlocked = false;
                skill.UpdateUI(true, GetAllSkillsInGroup(currentGroupName));
            }
        }

        playerStats.SkillPoints += pointsToReturn;
        OnSkillPointsChanged?.Invoke(playerStats.SkillPoints);
        OnSkillsUpdated?.Invoke();

        Debug.Log($"Сброшено навыков: {group.skills.Count(s => s.isUnlocked == false && s.canBeReset)}. Возвращено {pointsToReturn} очков.");
    }

    //
    // Resets question states and returns gold for purchased questions in current panel
    //
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
        playerStats.Gold += goldToReturn;
        OnGoldChanged?.Invoke(playerStats.Gold);
        OnSkillsUpdated?.Invoke();
    }

    //
    // Adds skill points to the player's total
    //
    public void AddSkillPoints(int points)
    {
        playerStats.SkillPoints += points;
        OnSkillPointsChanged?.Invoke(playerStats.SkillPoints);
    }

    //
    // Returns the current skill points available to the player
    //
    public int GetSkillPoints() => playerStats.SkillPoints;

    //
    // Returns all skills across all groups
    //
    public Skill[] GetAllSkills() => skillGroups.SelectMany(g => g.skills).ToArray();

    //
    // Returns all skills in a specific group by name
    //
    public Skill[] GetAllSkillsInGroup(string groupName) =>
        skillGroups.FirstOrDefault(g => g.groupName == groupName)?.skills ?? new Skill[0];

    //
    // Returns the current gold amount
    //
    public int GetGold() => playerStats.Gold;

    //
    // Returns all skill groups
    //
    public SkillGroup[] GetSkillGroups() => skillGroups;

    //
    // Sets the visibility of a skill and updates its UI state
    //
    public void SetSkillVisibility(string groupName, int skillIndex, bool isVisible)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null) return;

        Skill skill = group.skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null) return;

        skill.HandleVisibilityChange(isVisible, GetAllSkillsInGroup(groupName));
        OnSkillsUpdated?.Invoke();
    }

    //
    // Sets skill visibility with extended functionality for hidden panel unlocking
    //
    public void SetSkillVisibility1(string groupName, int skillIndex, bool isVisible)
    {
        SkillGroup group = skillGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null)
        {
            Debug.LogWarning($"Группа {groupName} не найдена!");
            return;
        }

        Skill skill = group.skills.FirstOrDefault(s => s.skillIndex == skillIndex);
        if (skill == null)
        {
            Debug.LogWarning($"Навык с индексом {skillIndex} в группе {groupName} не найден!");
            return;
        }

        skill.HandleVisibilityChange(isVisible, GetAllSkillsInGroup(groupName));
        if (SkillPanelManager.GetCurrentPanelName() == "Hidden")
        {
            Debug.Log($"Unlocking skill {skill.skillName} in Hidden panel.");
            skillTreeNavigation.CenterOnSkill(skill);
        }
        if (isVisible)
        {
            skillPanelUI?.UnlockPanel(groupName);
        }
        OnSkillsUpdated?.Invoke();
    }

    //
    // Resets a single skill and its dependent skills with notification
    //
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

        Action resetAction = () =>
        {
            ResetSkillUpgrades(groupName, skillIndex);
            skillToReset.isUnlocked = false;
            skillToReset.UpdateUI(true, GetAllSkillsInGroup(groupName));
            playerStats.SkillPoints += skillToReset.cost;
            OnSkillPointsChanged?.Invoke(playerStats.SkillPoints);
            OnSkillsUpdated?.Invoke();
            Debug.Log($"Сброшен навык: {skillToReset.skillName}. Возвращено {skillToReset.cost} очков.");
        };
        if (dependentSkills.Count > 0)
        {
            notificationManager?.ShowNotification(skillToReset, dependentSkills.Count, () =>
            {
                ResetSkillUpgrades(groupName, skillIndex);
                dependentSkills.Add(skillToReset);
                foreach (var skill in dependentSkills)
                {
                    ResetSkillUpgrades(groupName, skill.skillIndex);
                    skill.isUnlocked = false;
                    skill.UpdateUI(true, GetAllSkillsInGroup(groupName));
                }
                playerStats.SkillPoints += totalPointsToReturn;
                OnSkillPointsChanged?.Invoke(playerStats.SkillPoints);
                OnSkillsUpdated?.Invoke();
                Debug.Log($"Сброшены навыки: {string.Join(", ", dependentSkills.Select(s => s.skillName))}. Возвращено {totalPointsToReturn} очков.");
            });
        }
        else if (skillToReset.currentLevel > 1)
        {
            notificationManager?.ShowNotification(skillToReset, 0, resetAction);
        }
        else
        {
            resetAction.Invoke();
        }
    }

    //
    // Recursively finds all skills that depend on the given skill
    //
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

    //
    // Upgrades a skill to the next level if player has sufficient gold
    //
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

        if (!skill.CanUpgrade(playerStats.Gold))
        {
            Debug.LogWarning($"Недостаточно золота или навык {skill.skillName} уже на максимальном уровне!");
            skill.ShakeLockIcon(this);
            return;
        }

        playerStats.Gold -= skill.upgradeCosts[skill.currentLevel];
        skill.currentLevel++;
        OnGoldChanged?.Invoke(playerStats.Gold);
        OnSkillsUpdated?.Invoke();
        Debug.Log($"Навык {skill.skillName} улучшен до уровня {skill.currentLevel}. Потрачено {skill.upgradeCosts[skill.currentLevel - 1]} золота.");
    }

    //
    // Resets all upgrades for a skill and returns the gold spent on them
    //
    public void ResetSkillUpgrades(string groupName, int skillIndex)
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

        if (skill.currentLevel <= 1)
        {
            Debug.Log($"Навык {skill.skillName} не имеет улучшений для сброса!");
            return;
        }

        int goldToReturn = skill.upgradeCosts.Take(skill.currentLevel).Sum();
        playerStats.Gold += goldToReturn;

        skill.currentLevel = 1;
        OnGoldChanged?.Invoke(playerStats.Gold);
        OnSkillsUpdated?.Invoke();

        Debug.Log($"Улучшения для навыка {skill.skillName} сброшены. Возвращено {goldToReturn} золота.");
    }

    //
    // Downgrades a skill to a target level and returns excess upgrade gold
    //
    public void ResetSkillToLevel(string groupName, int skillIndex, int targetLevel)
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

        if (skill.currentLevel <= targetLevel)
        {
            Debug.LogWarning($"Навык {skill.skillName} уже на уровне {skill.currentLevel} или ниже!");
            return;
        }

        int goldToReturn = skill.upgradeCosts.Skip(targetLevel).Take(skill.currentLevel - targetLevel).Sum();
        playerStats.Gold += goldToReturn;

        skill.currentLevel = targetLevel;
        OnGoldChanged?.Invoke(playerStats.Gold);
        OnSkillsUpdated?.Invoke();

        Debug.Log($"Навык {skill.skillName} сброшен до уровня {targetLevel}. Возвращено {goldToReturn} золота.");
    }

    //
    // Finds and returns a skill by its name across all groups
    //
    public Skill GetSkillByName(string skillName)
    {
        return GetAllSkills().FirstOrDefault(s => s.skillName == skillName);
    }
}
