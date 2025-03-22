using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    private GameObject notificationPanelObject;
    private SkillNotificationPanel notificationPanel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeNotificationPanel();
    }

    private void InitializeNotificationPanel()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager ещё не инициализирован, откладываем инициализацию NotificationManager...");
            Invoke(nameof(InitializeNotificationPanel), 0.1f); // Отложенная инициализация
            return;
        }

        notificationPanelObject = UIManager.Instance.GetPanel(UIManager.PanelType.Notification);
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

    public void ShowNotification(Skill skill, ISkillTreeManager manager)
    {
        if (notificationPanel != null)
        {
            notificationPanel.ShowNotification(skill, manager);
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