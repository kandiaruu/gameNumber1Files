//
// Handles the display and lifecycle of skill notifications shown to the player
//

using UnityEngine;

public class SkillNotificationHandler : MonoBehaviour, ISkillNotificationHandler
{
    [InjectAttribute1] private INotificationManager notificationManager { get; set; }
    public bool skillNotificationPanelActive { get; set; } = false;

    //
    // Displays a notification for the given skill if notification manager is available
    //
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

    //
    // Marks the notification panel as closed
    //
    public void OnNotificationPanelClosed()
    {
        skillNotificationPanelActive = false;
        Debug.Log("Notification panel closed, skillNotificationPanelActive set to false");
    }

    //
    // Monitors if notification panel has been closed externally and resets flag accordingly
    //
    private void Update()
    {
        if (skillNotificationPanelActive && notificationManager != null && !notificationManager.IsNotificationActive())
        {
            skillNotificationPanelActive = false;
            Debug.Log("Notification panel was closed externally, resetting skillNotificationPanelActive to false");
        }
    }
}
