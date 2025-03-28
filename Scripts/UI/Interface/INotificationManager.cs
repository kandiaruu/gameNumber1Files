public interface INotificationManager
{
    void ShowNotification(Skill skill);
    bool IsNotificationActive();
    void CloseNotification();
}