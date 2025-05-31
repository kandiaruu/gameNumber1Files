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
        List<CraftingSlot> craftingSlots = new List<CraftingSlot>();

        foreach (var panel in allPanels)
        {
            if (panel.slots != null && panel.slots.Count > 0)
                combinedSlots.AddRange(panel.slots.ConvertAll(slot => slot as InventorySlot));
            
            if (panel.PanelType == InventoryPanelType.Crafting && panel.craftingSlots != null && panel.craftingSlots.Count > 0)
                craftingSlots.AddRange(panel.craftingSlots);
        }

        InventorySlot.HandleSlotNameSearch(combinedSlots);
        CraftingSlot.HandleSlotNameSearch(craftingSlots);

        // 👇 Обновление текста поиска
        if (searchTextDisplay != null)
        {
            searchTextDisplay.text = $"Search: {InventorySlot.getSearchInput()}";
        }
    }
}