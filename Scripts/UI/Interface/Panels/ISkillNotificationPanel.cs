public interface ISkillNotificationPanel : IPanel
{
    void ShowNotification(Skill skill, ISkillTreeManager skillTreeManager);
    void ShowResetConfirmation(Skill skill, int dependentCount, System.Action onConfirm); // Новый метод
}