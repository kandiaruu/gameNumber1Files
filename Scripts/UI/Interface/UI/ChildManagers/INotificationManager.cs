public interface INotificationManager
{
    void ShowNotification(Skill skill);
    bool IsNotificationActive();
    void CloseNotification();
    void ShowNotification(Skill skill, int dependentCount, System.Action onConfirm); // Новый метод
}