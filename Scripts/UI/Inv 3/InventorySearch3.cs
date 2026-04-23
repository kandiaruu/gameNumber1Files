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

private bool HandleTyping()
{
    string input = Input.inputString;
    bool changed = false;

    foreach (char c in input)
    {
        if (c == '\b') // Backspace
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

    private void StartSearch()
    {
        isSearching = true;
        currentQuery = "";
        searchText.color = activeColor;
        UpdateSearchText();
        ApplySearch();
    }

    public void StopSearch()
    {
        isSearching = false;
        currentQuery = "";
        searchText.color = inactiveColor;
        inventoryPanel3.SetSearchQuery("");
        UpdateSearchText();
    }

    public bool returnSearching()
    {
        return isSearching;
    }

    private void UpdateSearchText()
    {
        if (searchText == null) return;
        searchText.text = $"Search: {currentQuery}";
    }

    public void ApplySearch()
    {
        if (slotsParent == null || itemDatabase == null) return;
        if (currentQuery == lastAppliedQuery) return;

        lastAppliedQuery = currentQuery;
        inventoryPanel3.SetSearchQuery(currentQuery);
    }
}