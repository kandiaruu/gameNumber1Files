using UnityEngine;

public class NotificationManager : MonoBehaviour, INotificationManager
{
    [InjectAttribute1]
    private ISkillTreeManager skillLogicManager { get; set; }
    [InjectAttribute1]
    private ISkillNotificationPanel notificationPanel { get; set; }
    [InjectAttribute1]
    private IUIManager uiManager { get; set; }

    void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        InitializeNotificationPanel();
    }

    private void InitializeNotificationPanel()
    {
        GameObject notificationPanelObject = uiManager.GetPanel(UIManager.PanelType.Notification);
        if (notificationPanelObject != null)
        {
            notificationPanel = notificationPanelObject.GetComponent<SkillNotificationPanel>();
            if (notificationPanel == null)
            {
                Debug.LogError($"Панель {notificationPanelObject.name} (Notification) не реализует SkillNotificationPanel!");
            }
        }
        else
        {
            Debug.LogError("Панель типа Notification не найдена в кэше UIManager!");
        }
    }

    public void ShowNotification(Skill skill)
    {
        if (notificationPanel != null)
        {
            notificationPanel.ShowNotification(skill, skillLogicManager);
        }
        else
        {
            Debug.LogWarning("NotificationPanel не инициализирован!");
        }
    }

        public void ShowNotification(Skill skill, int dependentCount, System.Action onConfirm)
    {
        if (notificationPanel != null)
        {
            notificationPanel.ShowResetConfirmation(skill, dependentCount, onConfirm);
        }
        else
        {
            Debug.LogWarning("NotificationPanel не инициализирован!");
        }
    }

    public bool IsNotificationActive()
    {
        return notificationPanel != null && notificationPanel.IsOpen;
    }

    public void CloseNotification()
    {
        if (notificationPanel != null && notificationPanel.IsOpen)
        {
            notificationPanel.Close();
        }
    }
}