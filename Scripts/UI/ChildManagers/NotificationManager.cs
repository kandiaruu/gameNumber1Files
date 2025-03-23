using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    private SkillTreeManager skillTreeManager;
    private SkillNotificationPanel notificationPanel;
    private UIManager uiManager;

    void Awake()
    {
        DependencyContainer container = DependencyContainer.Instance;
        skillTreeManager = container.Resolve<SkillTreeManager>();
        uiManager = container.Resolve<UIManager>();

        if (skillTreeManager == null) Debug.LogError("SkillTreeManager не зарегистрирован!");
        if (uiManager == null) Debug.LogError("UIManager не зарегистрирован!");

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
            notificationPanel.ShowNotification(skill, skillTreeManager);
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