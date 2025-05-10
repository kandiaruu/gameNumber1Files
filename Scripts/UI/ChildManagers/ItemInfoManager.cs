using UnityEngine;

public class ItemInfoManager : MonoBehaviour, IItemInfoManager
{
    [InjectAttribute1] private IUIManager uiManager { get; set; }
    private IItemInfo itemInfoPanel;
    private void Start()
    {
        GameObject notificationPanelObject = uiManager.GetPanel(UIManager.PanelType.ItemInfo);
        if (notificationPanelObject != null)
        {
            itemInfoPanel = notificationPanelObject.GetComponent<ItemInfo>();
            if (itemInfoPanel == null)
            {
                Debug.LogError($"Панель {notificationPanelObject.name} (Notification) не реализует SkillNotificationPanel!");
            }
        }
        else
        {
            Debug.LogError("Панель типа Notification не найдена в кэше UIManager!");
        }
    }

    public void ShowItemInfo(Item item)
    {
        if (item != null && itemInfoPanel != null)
        {
            itemInfoPanel.SetItemInfo(item);
        }
    }

    public void HideItemInfo()
    {
        if (itemInfoPanel != null)
        {
            itemInfoPanel.Close();
        }
    }
}