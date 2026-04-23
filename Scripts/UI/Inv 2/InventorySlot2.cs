using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot2 : MonoBehaviour
{
    [SerializeField] public Image backgroundImage;
    [SerializeField] public Image itemImage;
    [SerializeField] private Image frameImage;
    [SerializeField] public TMP_Text stackText;
    public int itemId = -1;
    public int itemStackSize = 0;
    public bool isOccupied;
    public Vector2Int gridPosition;
    public InventoryPanel2 inventoryPanel;
    public bool highlightSameId;

    private ItemDatabase2 itemDatabase;

    public void SetDatabase(ItemDatabase2 db)
    {
        itemDatabase = db;
    }

    public void SetItem(int newItemId, int stackSize, ItemDatabase2 db = null)
    {
        itemId = newItemId;
        itemStackSize = stackSize;
        isOccupied = itemId >= 0;
        if (db != null) itemDatabase = db;
        UpdateVisual();
    }

    public void ClearSlot()
    {
        itemId = -1;
        itemStackSize = 0;
        isOccupied = false;
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