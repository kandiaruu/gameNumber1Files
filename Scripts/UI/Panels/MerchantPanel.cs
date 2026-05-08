//
// Panel for a simple merchant that sells lockpicks. The player enters a quantity,
// sees the total cost, and confirms the purchase which deducts gold and adds
// items to the inventory.
//

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public interface IMerchantPanel : IPanel { }

public class MerchantPanel : BasePanel, IMerchantPanel
{
    [InjectAttribute1] private IPlayerStats playerStats { get; set; }
    [InjectAttribute1] private IInventoryPanel3 inventoryPanel { get; set; }

    [Header("Trade")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI buyButtonText;

    private int lockpickItemId = 4;
    private int lockpickPrice = 20;
    private int currentAmount = 1;

    // Injects dependencies, configures the amount input field, wires the buy button, and refreshes the UI
    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);

        if (amountInput != null)
        {
            amountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            amountInput.characterLimit = 3;
            amountInput.onValueChanged.AddListener(OnInputChanged);
            amountInput.onEndEdit.AddListener(OnInputEndEdit);
        }

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);

        currentAmount = 1;
        if (amountInput != null) amountInput.text = "1";
        UpdateUI();
    }

    // Parses and clamps the typed value (1-100) and refreshes the cost display
    private void OnInputChanged(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (int.TryParse(text, out int val))
        {
            currentAmount = Mathf.Clamp(val, 1, 100);
            if (val > 100 && amountInput != null)
            {
                amountInput.SetTextWithoutNotify(currentAmount.ToString());
            }
            UpdateUI();
        }
    }

    // Resets the amount to 1 if the field is left empty or contains an invalid value on submit
    private void OnInputEndEdit(string text)
    {
        if (string.IsNullOrEmpty(text) || !int.TryParse(text, out int val) || val < 1)
        {
            currentAmount = 1;
            if (amountInput != null) amountInput.SetTextWithoutNotify(currentAmount.ToString());
            UpdateUI();
        }
    }

    // Refreshes the buy button label with the current total cost and the amount display text
    private void UpdateUI()
    {
        int totalCost = currentAmount * lockpickPrice;

        if (buyButtonText != null)
        {
            buyButtonText.text = $"Buy for {totalCost} gold";
        }

        if (amountText != null)
        {
            amountText.text = $"{currentAmount}";
        }
    }

    // Deducts gold and adds lockpicks to the player inventory if the player can afford the purchase
    private void OnBuyClicked()
    {
        if (playerStats == null || inventoryPanel == null) return;

        int totalCost = currentAmount * lockpickPrice;

        if (playerStats.Gold >= totalCost)
        {
            playerStats.Gold -= totalCost;
            bool success = inventoryPanel.TryAddItem(lockpickItemId, currentAmount);

            if (success)
            {
                Debug.Log($"Successfully bought {currentAmount} item(s) for {totalCost} gold!");
            }
        }
        else
        {
            Debug.LogWarning("Not enough gold!");
        }
    }

    // Resets the amount to 1, clears the input field, and closes the panel
    public override void Close()
    {
        currentAmount = 1;

        if (amountInput != null)
        {
            amountInput.SetTextWithoutNotify("1");
        }

        UpdateUI();
        base.Close();
    }
}
