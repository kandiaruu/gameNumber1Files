public interface ISkillTreeManager
{
    event System.Action<int> OnSkillPointsChanged;
    event System.Action<int> OnGoldChanged;
    event System.Action OnSkillsUpdated;

    void UnlockSkill(int skillIndex);
    void BuyQuestionState(int skillIndex);
    void ResetSkills();
    void ResetQuestionsAndGold();
    void AddSkillPoints(int points);
    int GetSkillPoints();
    Skill[] GetAllSkills();
    int GetGold();
    void EnableSkillButtons(bool enable);
    void OnNotificationPanelClosed();
}