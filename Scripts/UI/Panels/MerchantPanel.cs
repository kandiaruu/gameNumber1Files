using UnityEngine;
using UnityEngine.UI;
using TMPro;

public interface IMerchantPanel : IPanel { }

public class MerchantPanel : BasePanel, IMerchantPanel
{
    [InjectAttribute1] private IPlayerStats playerStats { get; set; }
    [InjectAttribute1] private IInventoryPanel3 inventoryPanel { get; set; }

    [Header("Торговля")]
    [SerializeField] private Button buyButton;            
    [SerializeField] private TMP_InputField amountInput;    
    [SerializeField] private TextMeshProUGUI amountText;  
    [SerializeField] private TextMeshProUGUI buyButtonText;     

    private int lockpickItemId = 4;
    private int lockpickPrice = 20;
    private int currentAmount = 1;

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

    private void OnInputEndEdit(string text)
    {
        if (string.IsNullOrEmpty(text) || !int.TryParse(text, out int val) || val < 1)
        {
            currentAmount = 1;
            if (amountInput != null) amountInput.SetTextWithoutNotify(currentAmount.ToString());
            UpdateUI();
        }
    }

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
                Debug.Log($"Успешно куплено {currentAmount} шт. за {totalCost} золота!");
            }
        }
        else
        {
            Debug.LogWarning("Не хватает золота!");
        }
    }

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