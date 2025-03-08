using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryPanel;
    public TMP_Text staminaText;
    private bool isInventoryOpen = false;
    private SettingsMenu settingsMenu;
    private ThirdPersonCharacter character;

    public bool IsInventoryOpen => isInventoryOpen;

    void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        settingsMenu = Object.FindFirstObjectByType<SettingsMenu>();
        if (settingsMenu == null)
        {
            Debug.LogError("Не найдено SettingsMenu в сцене!");
        }

        character = Object.FindFirstObjectByType<ThirdPersonCharacter>();
        if (character == null)
        {
            Debug.LogError("Не найден ThirdPersonCharacter в сцене!");
        }

        if (staminaText != null)
        {
            staminaText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isInventoryOpen && staminaText != null && character != null)
        {
            float staminaPercentage = character.GetStaminaPercentage() * 100f;
            int roundedStamina = Mathf.RoundToInt(staminaPercentage);
            staminaText.text = $"Стамина: {roundedStamina}%";
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);
        if (staminaText != null)
        {
            staminaText.gameObject.SetActive(isInventoryOpen);
        }
        UIStateManager.Instance.SetMenuState(isInventoryOpen, "Inventory");
    }
}