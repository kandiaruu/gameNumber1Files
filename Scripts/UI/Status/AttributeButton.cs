using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

//
// Reusable UI button component for a single player attribute entry.
// Displays the attribute name and current value, and invokes a callback when clicked.
//

public class AttributeButton : MonoBehaviour
{
    public TextMeshProUGUI attributeNameText;
    public TextMeshProUGUI attributeValueText;
    public Button button;
    private string attributeKey;
    private Action<string> onAttributeIncrease;

    // Populates the button with the given attribute data and wires up the increase callback
    public void Init(string attrKey, string attrDisplayName, int value, Action<string> onIncrease, bool interactable)
    {
        attributeKey = attrKey;
        attributeNameText.text = attrDisplayName;
        attributeValueText.text = value.ToString();
        onAttributeIncrease = onIncrease;
        button.interactable = interactable;
    }

    // Invokes the registered attribute-increase callback with this button's attribute key
    public void OnClick()
    {
        onAttributeIncrease?.Invoke(attributeKey);
    }
}
