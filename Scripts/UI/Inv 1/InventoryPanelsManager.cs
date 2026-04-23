using System.Collections.Generic;
using UnityEngine;

public class InventoryPanelsManager : MonoBehaviour, IInventoryPanelsManager
{
    public static InventoryPanelsManager Instance;

    private List<InventoryPanel> openPanels = new List<InventoryPanel>();

    [SerializeField] private InventoryPanel playerInventoryPanel;

    public List<InventoryPanel> OpenPanels => openPanels;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterOpenPanel(InventoryPanel panel)
    {
        if (!openPanels.Contains(panel))
            openPanels.Add(panel);
    }

    public void UnregisterPanel(InventoryPanel panel)
    {
        if (openPanels.Contains(panel))
            openPanels.Remove(panel);
    }

    public List<InventoryPanel> GetPanelsForDoubleClick()
    {
        var panels = new List<InventoryPanel>(openPanels);
        if (playerInventoryPanel != null && !panels.Contains(playerInventoryPanel))
            panels.Add(playerInventoryPanel);
        return panels;
    }
}
