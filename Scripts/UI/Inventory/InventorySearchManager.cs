using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventorySearchManager : MonoBehaviour
{
    public List<InventoryPanel> allPanels = new List<InventoryPanel>();
    
    public TextMeshProUGUI searchTextDisplay; // 👈 Добавлено

    private void Update()
    {
        List<InventorySlot> combinedSlots = new List<InventorySlot>();

        foreach (var panel in allPanels)
        {
            if (panel.slots != null && panel.slots.Count > 0)
                combinedSlots.AddRange(panel.slots);
        }

        InventorySlot.HandleSlotNameSearch(combinedSlots);

        // 👇 Обновление текста поиска
        if (searchTextDisplay != null)
        {
            searchTextDisplay.text = $"Search: {InventorySlot.getSearchInput()}";
        }
    }
}