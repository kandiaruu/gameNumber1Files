using UnityEngine;

public class SkillNotificationHandler : MonoBehaviour, ISkillNotificationHandler
{
    [InjectAttribute1] private INotificationManager notificationManager { get; set; }
    public bool skillNotificationPanelActive { get; set; } = false;

    public void ShowNotification(Skill skill)
    {
        if (notificationManager == null)
        {
            Debug.LogError("Нельзя показать уведомление: NotificationManager не инициализирован!");
            return;
        }
        notificationManager.ShowNotification(skill);
        skillNotificationPanelActive = true;
        Debug.Log("Notification panel shown, skillNotificationPanelActive set to true");
    }

    public void OnNotificationPanelClosed()
    {
        skillNotificationPanelActive = false;
        Debug.Log("Notification panel closed, skillNotificationPanelActive set to false");
    }

    private void Update()
    {
        // Fallback: If the notification panel is no longer active but the flag is still true, reset it
        if (skillNotificationPanelActive && notificationManager != null && !notificationManager.IsNotificationActive())
        {
            skillNotificationPanelActive = false;
            Debug.Log("Notification panel was closed externally, resetting skillNotificationPanelActive to false");
        }
    }
}