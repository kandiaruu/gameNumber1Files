public interface ISkillTreeManager
{
    void UnlockSkill(string groupName, int skillIndex);
    void BuyQuestionState(string groupName, int skillIndex);
    void ResetSkills(); // Старая версия
    void ResetQuestionsAndGold(); // Старая версия
    void AddSkillPoints(int points);
    int GetSkillPoints();
    Skill[] GetAllSkills();
    Skill[] GetAllSkillsInGroup(string groupName);
    int GetGold();
    SkillGroup[] GetSkillGroups();
    void EnableSkillButtons(bool enable);
    void OnNotificationPanelClosed();
    event System.Action<int> OnSkillPointsChanged;
    event System.Action<int> OnGoldChanged;
    event System.Action OnSkillsUpdated;
    void SetSkillVisibility(string groupName, int skillIndex, bool isVisible);
    void ResetSkill(string groupName, int skillIndex); // Новый метод
    void UpgradeSkill(string groupName, int skillIndex); // Новый метод
}