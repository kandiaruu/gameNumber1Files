using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class Skill
{
    public string skillName;
    public int skillIndex;
    public int cost;
    public bool isUnlocked;
    public int[] prerequisiteIndices;
    public bool isVisible = false; // Флаг видимости навыка
    public bool canBeReset = true;
    [SerializeField] public int requiredPrerequisiteCount;

    public Button skillButton;
    public GameObject lockIcon;

    [System.NonSerialized] public Vector3 originalPosition;
    [System.NonSerialized] public RectTransform lockIconTransform;
    [System.NonSerialized] public bool isShaking = false;
    [System.NonSerialized] private Image lockImage;
    [System.NonSerialized] private Color originalColor;

    public GameObject questionIcon;
    [System.NonSerialized] public bool hasQuestionState = false;
    [SerializeField] public bool hasQuestionByDefault = true;
    public int questionGoldCost = 5;

    [TextArea] public string description;
    public string[] characteristics;
    public int maxUpgrades;

    // Новое поле для хранения имени группы
    [System.NonSerialized] public string groupName;

    public void Initialize()
    {
        if (skillButton == null)
        {
            Debug.LogError($"skillButton не назначен для навыка {skillName}");
            return;
        }
        if (lockIcon == null || questionIcon == null)
        {
            Debug.LogError($"lockIcon или questionIcon не назначены для навыка {skillName}");
            return;
        }

        lockIconTransform = lockIcon.GetComponent<RectTransform>();
        if (lockIconTransform != null)
        {
            originalPosition = lockIconTransform.anchoredPosition;
        }

        hasQuestionState = !hasQuestionByDefault;
    }

    public Skill()
    {
        requiredPrerequisiteCount = prerequisiteIndices != null ? prerequisiteIndices.Length : 0;
    }

    public bool CanUnlock(Skill[] groupSkills)
    {
        if (isUnlocked) return false;

        if (prerequisiteIndices.Length == 0) return true;

        int unlockedCount = 0;
        foreach (int index in prerequisiteIndices)
        {
            Skill prereqSkill = System.Array.Find(groupSkills, s => s.skillIndex == index);
            if (prereqSkill != null && prereqSkill.isUnlocked)
            {
                unlockedCount++;
            }
        }

        return unlockedCount >= requiredPrerequisiteCount;
    }

    public void UpdateUI(bool canAfford, Skill[] allSkills)
    {
        bool canUnlock = CanUnlock(allSkills);
        lockIcon.SetActive(!isUnlocked && hasQuestionState && !canUnlock);
        questionIcon.SetActive(!isUnlocked && !hasQuestionState);

        TextMeshProUGUI buttonText = skillButton.GetComponentInChildren<TextMeshProUGUI>();
        if (isUnlocked)
            buttonText.text = "Разблокировано";
        else if (canUnlock && hasQuestionState)
            buttonText.text = skillName;
        else
            buttonText.text = "";

        skillButton.gameObject.SetActive(isVisible); // Устанавливаем видимость на основе флага
    }

    public void ShakeLockIcon(MonoBehaviour manager)
    {
        if (isShaking || lockIconTransform == null) return;
        if (lockImage == null) lockImage = lockIcon.GetComponent<Image>();
        if (lockImage == null)
        {
            Debug.LogError($"Компонент Image не найден на {lockIcon.name} для {skillName}!");
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

    public void HandleVisibilityChange(bool newVisibility, Skill[] groupSkills)
    {
        if (!isVisible && newVisibility)
        {
            if (CanUnlock(groupSkills))
            {
                isUnlocked = true;
            }
        }
        isVisible = newVisibility;
        // Обновляем UI при изменении видимости
        UpdateUI(true, groupSkills);
    }
}