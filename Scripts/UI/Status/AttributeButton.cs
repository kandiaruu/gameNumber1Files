using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AttributeButton : MonoBehaviour
{
    public TextMeshProUGUI attributeNameText;
    public TextMeshProUGUI attributeValueText;
    public Button button;
    private string attributeKey;
    private Action<string> onAttributeIncrease;

    public void Init(string attrKey, string attrDisplayName, int value, Action<string> onIncrease, bool interactable)
    {
        attributeKey = attrKey;
        attributeNameText.text = attrDisplayName;
        attributeValueText.text = value.ToString();
        onAttributeIncrease = onIncrease;
        button.interactable = interactable;
    }

    public void OnClick()
    {
        onAttributeIncrease?.Invoke(attributeKey);
    }
}