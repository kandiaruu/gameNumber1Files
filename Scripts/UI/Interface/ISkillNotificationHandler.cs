public interface ISkillNotificationHandler
{
    bool skillNotificationPanelActive { get; }
    void ShowNotification(Skill skill);
    void OnNotificationPanelClosed();
}