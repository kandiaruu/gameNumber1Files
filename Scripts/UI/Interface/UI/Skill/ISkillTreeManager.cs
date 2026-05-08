public interface ISkillTreeManager
{
    void UnlockSkill(string groupName, int skillIndex);
    void BuyQuestionState(string groupName, int skillIndex);
    void ResetSkills();
    void ResetQuestionsAndGold();
    void AddSkillPoints(int points);
    int GetSkillPoints();
    Skill[] GetAllSkills();
    Skill[] GetAllSkillsInGroup(string groupName);
    int GetGold();
    SkillGroup[] GetSkillGroups();
    event System.Action<int> OnSkillPointsChanged;
    event System.Action<int> OnGoldChanged;
    event System.Action OnSkillsUpdated;
    void SetSkillVisibility(string groupName, int skillIndex, bool isVisible);
    void ResetSkill(string groupName, int skillIndex); // Новый метод
    void UpgradeSkill(string groupName, int skillIndex); // Новый метод
    void ResetSkillUpgrades(string groupName, int skillIndex);
    void ResetSkillToLevel(string groupName, int skillIndex, int targetLevel);
    Skill GetSkillByName(string skillName);
}