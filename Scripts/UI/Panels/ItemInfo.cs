using UnityEngine;
using TMPro;

public class ItemInfo : BasePanel, IItemInfo
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text descriptionText;

    private Item currentItem;

    public override void Awake()
    {
        base.Awake();
        if (nameText == null || idText == null || descriptionText == null)
        {
            Debug.LogError("One or more Text components are not assigned in ItemInfo panel!");
        }
    }

    public void SetItemInfo(Item item)
    {
        currentItem = item;
        if (item != null)
        {
            nameText.text = $"Name: {item.itemName}";
            idText.text = $"ID: {item.id}";
            descriptionText.text = $"Description: {item.description}";
            Open();
        }
        else
        {
            Close();
        }
    }

    public override void Open()
    {
        base.Open();
        UpdateUI();
    }

    public override void Close()
    {
        base.Close();
        currentItem = null;
        ClearUI();
    }

    private void UpdateUI()
    {
        if (currentItem != null)
        {
            nameText.text = $"Name: {currentItem.itemName}";
            idText.text = $"ID: {currentItem.id}";
            descriptionText.text = $"Description: {currentItem.description}";
        }
    }

    private void ClearUI()
    {
        nameText.text = "";
        idText.text = "";
        descriptionText.text = "";
    }
}