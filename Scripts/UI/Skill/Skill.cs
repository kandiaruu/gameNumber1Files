using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public enum SkillCategory { Active, Passive } // Убрали Stat

[System.Serializable]
public class SkillLevelData
{
    [Header("Бонусы к урону (для Пассивок)")]
    public float flatDamageBonus;       // Плоский урон (например, 20)
    public float percentDamageBonus;    // Процентный урон (например, 0.1 это +10%)
    public float maxPercentDamageBonus; // Итоговый мультипликатор (например, 0.1 это +10% ко ВСЕМУ)

    [Header("Эффекты при попадании (для Пассивок)")]
    public float effectDamage;          // Урон от поджога/яда в секунду
    public float effectDuration;        // Длительность эффекта в секундах
}

[System.Serializable]
public class Skill
{
    public string skillName;
    public int skillIndex;
    public SkillCategory category;      // Тип навыка
    public List<string> tags;           // Теги (например: "Magic", "Fire")
    public SkillLevelData[] levelStats; // Настройки для каждого уровня (размер должен совпадать с maxUpgrades)
    public Sprite skillIcon;
    public GameObject skillPrefab;      
    public AudioClip castSound;    
    public int cost;
    public bool isUnlocked;
    public int[] prerequisiteIndices;
    public bool isVisible = false;
    public bool canBeReset = true;
    [SerializeField] public int requiredPrerequisiteCount;

    public Button skillButton;
    public GameObject lockIcon;
    [SerializeField] public Image upgradeRingBackground;
    [SerializeField] public Image upgradeRingFill;

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
    [SerializeField] public int maxUpgrades;
    [SerializeField] public int currentLevel = 0;
    [SerializeField] public List<int> upgradeCosts;


    [System.NonSerialized] public string groupName;

    // Список UI линий, соединяющих навык с его зависимостями
    private List<Image> dependencyLines = new List<Image>();

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

        if (maxUpgrades > 0 && (upgradeCosts == null || upgradeCosts.Count != maxUpgrades))
        {
            Debug.LogWarning($"Для навыка {skillName} maxUpgrades = {maxUpgrades}, но upgradeCosts не соответствует. Исправляем...");
            upgradeCosts = new List<int>(new int[maxUpgrades]);
        }
        UpdateRingUI();
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
        if (isUnlocked && currentLevel == maxUpgrades)
            buttonText.text = "MAX LVLEL";
        else if (isUnlocked && currentLevel > 0)
            buttonText.text = "Unlocked";
        else if (canUnlock && hasQuestionState)
            buttonText.text = skillName;
        else
            buttonText.text = "";

        skillButton.gameObject.SetActive(isVisible);
        UpdateRingUI();
        UpdateDependencyLines(allSkills);
    }

    private void UpdateRingUI()
    {
        if (upgradeRingBackground == null || upgradeRingFill == null) return;

        bool showRing = maxUpgrades >= 0;
        upgradeRingBackground.gameObject.SetActive(showRing);
        upgradeRingFill.gameObject.SetActive(showRing);

        if (!showRing)
        {
            upgradeRingFill.fillAmount = 0f;
            return;
        }

        float fillAmount = (float)currentLevel / maxUpgrades;
        upgradeRingFill.fillAmount = fillAmount;

        if (currentLevel == maxUpgrades && isUnlocked)
        {
            Color customColor;
            ColorUtility.TryParseHtmlString("#feda24", out customColor);
            upgradeRingFill.color = customColor;
        }
        else if (currentLevel > 0 && isUnlocked)
            upgradeRingFill.color = Color.blue;
        else
            upgradeRingFill.color = Color.white;
    }

    public bool CanUpgrade(int availableGold)
    {
        if (!isUnlocked || maxUpgrades == 0 || currentLevel >= maxUpgrades) return false;
        return availableGold >= upgradeCosts[currentLevel];
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
        UpdateUI(true, groupSkills);
    }
    
public void SetupDependencyLines(Skill[] groupSkills, GameObject lineParentObject)
{
    // Debug.Log($"Создаём линии для навыка {skillName}");

    foreach (var line in dependencyLines)
    {
        if (line != null) Object.Destroy(line.gameObject);
    }
    dependencyLines.Clear();

    if (prerequisiteIndices == null || prerequisiteIndices.Length == 0)
    {
        // Debug.Log($"Нет зависимостей для навыка {skillName}");
        return;
    }

    foreach (int prereqIndex in prerequisiteIndices)
    {
        Skill prereqSkill = System.Array.Find(groupSkills, s => s.skillIndex == prereqIndex);
        if (prereqSkill == null || prereqSkill.skillButton == null)
        {
            Debug.LogWarning($"Зависимый навык с индексом {prereqIndex} не найден для {skillName}");
            continue;
        }

        GameObject lineObject = new GameObject($"Line_{skillName}_to_{prereqSkill.skillName}");
        lineObject.transform.SetParent(lineParentObject.transform, false); // Оставляем под RectMask2D
        RectTransform lineRect = lineObject.AddComponent<RectTransform>();
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = Vector2.zero;

        Image lineImage = lineObject.AddComponent<Image>();
        lineImage.color = Color.gray;
        // Устанавливаем тип Image в Simple, чтобы избежать растяжения
        lineImage.type = Image.Type.Simple;
        // Убираем спрайт, чтобы Image был просто цветным прямоугольником
        lineImage.sprite = null;
        lineImage.raycastTarget = false; // Не нужно обрабатывать клики

        dependencyLines.Add(lineImage);
    }

    UpdateDependencyLines(groupSkills);
}

void UpdateDependencyLines(Skill[] groupSkills)
{
    if (dependencyLines.Count == 0 || prerequisiteIndices == null)
    {
        // Debug.Log($"Нет линий для {skillName}: dependencyLines.Count = {dependencyLines.Count}, prerequisiteIndices = {prerequisiteIndices?.Length}");
        return;
    }

    int lineIndex = 0;
    foreach (int prereqIndex in prerequisiteIndices)
    {
        if (lineIndex >= dependencyLines.Count) break;

        Skill prereqSkill = System.Array.Find(groupSkills, s => s.skillIndex == prereqIndex);
        if (prereqSkill == null || prereqSkill.upgradeRingBackground == null || upgradeRingBackground == null)
        {
            dependencyLines[lineIndex].gameObject.SetActive(false);
            lineIndex++;
            continue;
        }

        bool showLine = isVisible && prereqSkill.isVisible;
        dependencyLines[lineIndex].gameObject.SetActive(showLine);

        if (showLine)
        {
            // Используем World-позиции кольца
            RectTransform fromRect = prereqSkill.upgradeRingBackground.rectTransform;
            RectTransform toRect = upgradeRingBackground.rectTransform;

            Vector3 fromWorld = fromRect.position;
            Vector3 toWorld = toRect.position;

            // Направление
            Vector3 direction = (toWorld - fromWorld).normalized;

            // Радиусы с учетом масштаба
            float fromRadiusX = fromRect.rect.width * 0.5f * fromRect.lossyScale.x;
            float fromRadiusY = fromRect.rect.height * 0.5f * fromRect.lossyScale.y;
            float toRadiusX = toRect.rect.width * 0.5f * toRect.lossyScale.x;
            float toRadiusY = toRect.rect.height * 0.5f * toRect.lossyScale.y;

            Vector3 offsetFrom = new Vector3(direction.x * fromRadiusX, direction.y * fromRadiusY, 0);
            Vector3 offsetTo = new Vector3(direction.x * toRadiusX, direction.y * toRadiusY, 0);

            Vector3 worldStart = fromWorld + offsetFrom;
            Vector3 worldEnd = toWorld - offsetTo;

            // Получаем LineContainer (где лежат линии)
            RectTransform lineParent = dependencyLines[lineIndex].rectTransform.parent as RectTransform;

            // Переводим в локальные координаты LineContainer
            Vector2 localStart = lineParent.InverseTransformPoint(worldStart);
            Vector2 localEnd = lineParent.InverseTransformPoint(worldEnd);

            // Строим линию
            RectTransform lineRect = dependencyLines[lineIndex].rectTransform;
            Vector2 delta = localEnd - localStart;
            float distance = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            lineRect.anchoredPosition = (localStart + localEnd) / 2f;
            lineRect.sizeDelta = new Vector2(distance, 7f);
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);

            // Устанавливаем цвет линии
            if (isUnlocked && prereqSkill.isUnlocked && currentLevel == maxUpgrades && prereqSkill.currentLevel == prereqSkill.maxUpgrades)
            {
                Color customColor;
                ColorUtility.TryParseHtmlString("#feda24", out customColor);
                dependencyLines[lineIndex].color = customColor;
            }
            else if (isUnlocked && prereqSkill.isUnlocked)
            {
                dependencyLines[lineIndex].color = Color.blue;
            }
            else
            {
                dependencyLines[lineIndex].color = Color.white;
            }

            // Debug.Log($"Линия {skillName} <- {prereqSkill.skillName}: ({localStart}) -> ({localEnd})");
        }

        lineIndex++;
    }
}


}