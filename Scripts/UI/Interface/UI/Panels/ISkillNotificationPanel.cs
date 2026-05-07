public interface ISkillNotificationPanel : IPanel
{
    void ShowUpgradeResetNotification(Skill skill, ISkillTreeManager skillTreeManager);
    void ShowNotification(Skill skill, ISkillTreeManager skillTreeManager);
    void ShowResetConfirmation(Skill skill, int dependentCount, System.Action onConfirm); // Новый метод
    void ShowMessage(string message);
}