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

    [SerializeField] private int requiredPrerequisiteCount; // Новое поле: сколько требований нужно выполнить

    public Button skillButton;
    public GameObject lockIcon;

    [SerializeField] private SkillTreeNavigation skillTreeNavigation; // Ссылка на скрипт SkillTreeNavigation
    [System.NonSerialized] public Vector3 originalPosition;
    [System.NonSerialized] public RectTransform lockIconTransform;
    [System.NonSerialized] public bool isShaking = false;
    [System.NonSerialized] private Image lockImage;
    [System.NonSerialized] private Color originalColor;

    public GameObject questionIcon; // Новый объект для знака ?
    [System.NonSerialized] public bool hasQuestionState = false; // Флаг состояния ?
    [SerializeField] public bool hasQuestionByDefault = true; // Флаг по умолчанию для ?
    public int questionGoldCost = 5; // Индивидуальная стоимость золота для покупки ?

    [TextArea] public string description;
    public string[] characteristics;
    public int maxUpgrades;

    // Конструктор для установки начального значения requiredPrerequisiteCount
    public Skill()
    {
        // Устанавливаем значение по умолчанию в конструкторе
        requiredPrerequisiteCount = prerequisiteIndices != null ? prerequisiteIndices.Length : 0;
    }

    public bool CanUnlock(Skill[] allSkills)
    {
        if (isUnlocked) return false;

        if (prerequisiteIndices.Length == 0) return true; // Если нет требований, навык можно разблокировать

        // Подсчитываем количество разблокированных обязательных навыков
        int unlockedCount = 0;
        foreach (int index in prerequisiteIndices)
        {
            Skill prereqSkill = System.Array.Find(allSkills, s => s.skillIndex == index);
            if (prereqSkill != null && prereqSkill.isUnlocked)
            {
                unlockedCount++;
            }
        }

        // Сравниваем с требуемым количеством
        return unlockedCount >= requiredPrerequisiteCount;
    }

    public void UpdateUI(bool canAfford, Skill[] allSkills)
    {
        bool canUnlock = CanUnlock(allSkills);
        lockIcon.SetActive(!isUnlocked && hasQuestionState && !canUnlock); // Замок появляется только после ? и если условия не выполнены
        questionIcon.SetActive(!isUnlocked && !hasQuestionState); // ? показывается, если не куплено и навык не разблокирован

        TextMeshProUGUI buttonText = skillButton.GetComponentInChildren<TextMeshProUGUI>();
        if (isUnlocked) buttonText.text = "Разблокировано";
        else if (canUnlock && hasQuestionState) buttonText.text = cost.ToString(); // Текст стоимости отображается только после покупки ?
        else buttonText.text = ""; // Если ? не куплен, текст пустой
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
}

// в инспекторе можно назанчить сколько "обязательных требований" нужно выполнить для разблокировки навыка