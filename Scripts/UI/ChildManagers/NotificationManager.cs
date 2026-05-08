using UnityEngine;

//
// Manages the skill notification panel: resolves it via UIManager and exposes
// methods to show skill unlock notifications, reset confirmations, and plain messages.
//

public class NotificationManager : MonoBehaviour, INotificationManager
{
    [InjectAttribute1]
    private ISkillTreeManager skillLogicManager { get; set; }
    [InjectAttribute1]
    private ISkillNotificationPanel notificationPanel { get; set; }
    [InjectAttribute1]
    private IUIManager uiManager { get; set; }

    // Injects dependencies and initializes the notification panel reference
    void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        InitializeNotificationPanel();
    }

    // Retrieves the Notification panel from UIManager and caches the ISkillNotificationPanel component
    private void InitializeNotificationPanel()
    {
        GameObject notificationPanelObject = uiManager.GetPanel(UIManager.PanelType.Notification);
        if (notificationPanelObject != null)
        {
            notificationPanel = notificationPanelObject.GetComponent<SkillNotificationPanel>();
            if (notificationPanel == null)
            {
                Debug.LogError($"Panel {notificationPanelObject.name} (Notification) does not implement SkillNotificationPanel!");
            }
        }
        else
        {
            Debug.LogError("Panel of type Notification not found in UIManager cache!");
        }
    }

    // Shows a skill unlock or status notification for the given skill
    public void ShowNotification(Skill skill)
    {
        if (notificationPanel != null)
        {
            notificationPanel.ShowNotification(skill, skillLogicManager);
        }
        else
        {
            Debug.LogWarning("NotificationPanel is not initialized!");
        }
    }

    // Shows a reset confirmation dialog indicating how many dependent skills will be affected
    public void ShowNotification(Skill skill, int dependentCount, System.Action onConfirm)
    {
        if (notificationPanel != null)
        {
            notificationPanel.ShowResetConfirmation(skill, dependentCount, onConfirm);
        }
        else
        {
            Debug.LogWarning("NotificationPanel is not initialized!");
        }
    }

    // Returns true if the notification panel is currently open
    public bool IsNotificationActive()
    {
        return notificationPanel != null && notificationPanel.IsOpen;
    }

    // Closes the notification panel if it is currently open
    public void CloseNotification()
    {
        if (notificationPanel != null && notificationPanel.IsOpen)
        {
            notificationPanel.Close();
        }
    }

    // Displays a plain text message in the notification panel
    public void ShowMessage(string message)
    {
        if (notificationPanel != null)
        {
            notificationPanel.ShowMessage(message);
        }
        else
        {
            Debug.LogWarning("NotificationPanel is not initialized!");
        }
    }
}
