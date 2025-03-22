public interface ISkillTreeManager
{
    void UnlockSkill(int skillIndex);
    void BuyQuestionState(int skillIndex);
    int GetSkillPoints();
    Skill[] GetAllSkills();
    void EnableSkillButtons(bool enable);
    void OnNotificationPanelClosed();
}