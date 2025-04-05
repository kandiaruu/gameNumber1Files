public interface ISkillNotificationPanel : IPanel
{
    void ShowNotification(Skill skill, ISkillTreeManager skillTreeManager);
}