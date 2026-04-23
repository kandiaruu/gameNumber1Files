using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot3 : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] public Image backgroundImage;
    [SerializeField] public Image itemImage;
    [SerializeField] private Image frameImage;
    [SerializeField] public TMP_Text stackText;
    public int itemId = -1;
    public int itemStackSize = 0;
    public long firstAddedTicks;
    public long lastAddedTicks;
    public bool isOccupied;
    public Vector2Int gridPosition;
    public InventoryPanel3 inventoryPanel;

    private ItemDatabase3 itemDatabase;
    public string itemInstanceId;

    public void SetDatabase(ItemDatabase3 db)
    {
        itemDatabase = db;
    }

    public void SetItem(string instanceId, int newItemId, int stackSize, ItemDatabase3 db = null)
    {
        itemInstanceId = instanceId;
        itemId = newItemId;
        itemStackSize = stackSize;
        isOccupied = itemId >= 0;
        if (db != null) itemDatabase = db;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (itemImage != null)
        {
            Sprite icon = null;
            if (itemId >= 0 && itemDatabase != null)
            {
                var item = itemDatabase.GetItemById(itemId, itemStackSize);
                icon = item != null ? item.icon : null;
            }
            itemImage.sprite = icon;
            itemImage.enabled = icon != null;
        }
        if (stackText != null)
        {
            if (itemStackSize > 1)
            {
                stackText.text = itemStackSize.ToString();
                stackText.enabled = true;
            }
            else
            {
                stackText.text = "";
                stackText.enabled = false;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        
        if (eventData.button == PointerEventData.InputButton.Right)
            inventoryPanel.OnSlotLeftClick(this);
    }

    // public void OnPointerClick(PointerEventData eventData)
    // {
    //      if (eventData.button == PointerEventData.InputButton.Left)
    //     {
    //         inventoryPanel.OnSlotLeftClick(this);
    //     }
    //     else if (eventData.button == PointerEventData.InputButton.Right)
    //     {
    //         inventoryPanel.OnSlotRightClick(this);
    //     }
    // }
}