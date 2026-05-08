//
// Handles keyboard-driven item search within the inventory. Pressing Enter toggles
// search mode on/off; while active, typed characters build a query string that is
// forwarded to InventoryPanel3 to filter the displayed slots in real time.
//

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventorySearch3 : MonoBehaviour, IInventorySearch3
{
    [InjectAttribute1] private IInventoryPanel3 inventoryPanel3 { get; set; }
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI searchText;

    [Header("Inventory")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private ItemDatabase3 itemDatabase;

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.black;
    [SerializeField] private Color inactiveColor = Color.white;

    private bool isSearching = false;
    private string currentQuery = "";
    private string lastAppliedQuery = "";
    private const int maxSearchLength = 32;

    // Checks each frame for Enter/Escape keypresses to toggle search mode, and processes typed input while searching
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (isSearching)
                StopSearch();
            else
                StartSearch();

            return;
        }

        if (isSearching && Input.GetKeyDown(KeyCode.Escape))
        {
            StopSearch();
            return;
        }

        if (isSearching)
        {
            bool changed = HandleTyping();
            if (changed)
            {
                UpdateSearchText();
                ApplySearch();
            }
        }
    }

    // Processes raw keyboard input to append characters to or remove characters from the current query; returns true if the query changed
    private bool HandleTyping()
    {
        string input = Input.inputString;
        bool changed = false;

        foreach (char c in input)
        {
            if (c == '\b')
            {
                if (currentQuery.Length > 0)
                {
                    currentQuery = currentQuery.Substring(0, currentQuery.Length - 1);
                    changed = true;
                }
            }
            else if (!char.IsControl(c))
            {
                if (currentQuery.Length < maxSearchLength)
                {
                    currentQuery += c;
                    changed = true;
                }
            }
        }

        return changed;
    }

    // Activates search mode, resets the query, and applies an empty filter
    private void StartSearch()
    {
        isSearching = true;
        currentQuery = "";
        searchText.color = activeColor;
        UpdateSearchText();
        ApplySearch();
    }

    // Deactivates search mode, clears the query, and removes the filter from the inventory panel
    public void StopSearch()
    {
        isSearching = false;
        currentQuery = "";
        searchText.color = inactiveColor;
        inventoryPanel3.SetSearchQuery("");
        UpdateSearchText();
    }

    // Returns whether the search input is currently active
    public bool returnSearching()
    {
        return isSearching;
    }

    // Refreshes the search label text to show the current query
    private void UpdateSearchText()
    {
        if (searchText == null) return;
        searchText.text = $"Search: {currentQuery}";
    }

    // Sends the current query to the inventory panel if it has changed since the last apply
    public void ApplySearch()
    {
        if (slotsParent == null || itemDatabase == null) return;
        if (currentQuery == lastAppliedQuery) return;

        lastAppliedQuery = currentQuery;
        inventoryPanel3.SetSearchQuery(currentQuery);
    }
}
