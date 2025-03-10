using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class Skill
{
    public string skillName;
    public int skillIndex; // Новое поле для задания индекса в инспекторе
    public int cost;
    public bool isUnlocked;
    public int[] prerequisiteIndices;
    public Button skillButton;
    public GameObject lockIcon;

    [System.NonSerialized] public Vector3 originalPosition;
    [System.NonSerialized] public RectTransform lockIconTransform;
    [System.NonSerialized] public bool isShaking = false;
    [System.NonSerialized] private Image lockImage;
    [System.NonSerialized] private Color originalColor;

    public bool CanUnlock(Skill[] allSkills)
    {
        if (isUnlocked) return false;
        foreach (int index in prerequisiteIndices)
        {
            // Ищем навык с соответствующим skillIndex
            Skill prereqSkill = System.Array.Find(allSkills, s => s.skillIndex == index);
            if (prereqSkill == null || !prereqSkill.isUnlocked)
            {
                return false;
            }
        }
        return true;
    }

    public void UpdateUI(bool canAfford, Skill[] allSkills)
    {
        bool canUnlock = CanUnlock(allSkills);
        lockIcon.SetActive(!isUnlocked && !canUnlock); // Иконка замка видна, если навык не разблокирован и условия не выполнены

        TextMeshProUGUI buttonText = skillButton.GetComponentInChildren<TextMeshProUGUI>();
        if (isUnlocked)
        {
            buttonText.text = "Unlocked"; // Если навык разблокирован, показываем "Unlocked"
        }
        else if (canUnlock)
        {
            buttonText.text = cost.ToString(); // Если навык можно разблокировать, показываем его стоимость
        }
        else
        {
            buttonText.text = ""; // Если условия не выполнены, текст пустой
        }
    }

    public void ShakeLockIcon(MonoBehaviour manager)
    {
        if (isShaking || lockIconTransform == null) return;
        if (lockImage == null) lockImage = lockIcon.GetComponent<Image>();
        if (lockImage == null)
        {
            Debug.LogError($"No Image component found on {lockIcon.name} for {skillName}!");
            return;
        }
        originalColor = lockImage.color;
        isShaking = true;
        lockIconTransform.anchoredPosition = originalPosition;
        manager.StartCoroutine(ShakeAnimation());
    }

    private System.Collections.IEnumerator ShakeAnimation()
    {
        float duration = 0.3f;
        float amplitude = 10f;
        float frequency = 30f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float offsetX = amplitude * Mathf.Sin(elapsed * frequency);
            lockIconTransform.anchoredPosition = originalPosition + new Vector3(offsetX, 0, 0);
            if (lockImage != null)
                lockImage.color = Color.Lerp(originalColor, Color.red, Mathf.Abs(Mathf.Sin(elapsed * frequency)));
            yield return null;
        }

        lockIconTransform.anchoredPosition = originalPosition;
        if (lockImage != null) lockImage.color = originalColor;
        isShaking = false;
    }
}