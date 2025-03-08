using UnityEngine;
using UnityEngine.UI; // Для компонента Button
using TMPro;
using System.Collections;

public class skilltree : MonoBehaviour
{
    public GameObject SkillTreePanel; // Панель дерева навыков
    public TMP_Text skillPointsText; // Текст для отображения очков навыков
    private int skillPoints = 2; // Начальное количество очков
    public bool isMenuOpen = false; // Флаг состояния меню

    // Ссылки на кнопки, замок и текст
    public GameObject sprintButton; // Кнопка для навыка Sprint
    public GameObject doubleJumpButton; // Кнопка для навыка DoubleJump
    public GameObject doubleJumpLock; // Объект замка на кнопке DoubleJump
    public GameObject doubleJumpTextObject; // Объект текста (TMP) внутри кнопки DoubleJump

    private bool isSprintUnlocked = false; // Флаг разблокировки Sprint
    private bool isDoubleJumpUnlocked = false; // Флаг разблокировки DoubleJump
    private Button sprintButtonComponent; // Компонент кнопки Sprint
    private Button doubleJumpButtonComponent; // Компонент кнопки DoubleJump
    private RectTransform lockRectTransform; // Для управления позицией замка
    private bool isShaking = false; // Флаг для отслеживания состояния анимации

    void Start()
    {
        // Инициализация UI
        if (SkillTreePanel != null)
        {
            SkillTreePanel.SetActive(false); // Панель изначально СКРЫТА
            Debug.Log("SkillTreePanel инициализирован и СКРЫТ.");
        }
        else
        {
            Debug.LogError("SkillTreePanel не назначен в инспекторе!");
        }

        if (doubleJumpLock != null)
        {
            doubleJumpLock.SetActive(true); // Замок активен изначально
            Debug.Log("doubleJumpLock активирован в Start.");
            lockRectTransform = doubleJumpLock.GetComponent<RectTransform>(); // Получаем RectTransform
            if (lockRectTransform == null)
            {
                Debug.LogError("RectTransform не найден на doubleJumpLock!");
            }
            else
            {
                Debug.Log("RectTransform успешно найден на doubleJumpLock.");
            }
        }
        else
        {
            Debug.LogError("doubleJumpLock не назначен в инспекторе!");
        }

        if (doubleJumpTextObject != null)
        {
            doubleJumpTextObject.SetActive(false); // Скрываем текст DoubleJump изначально
            Debug.Log("Текст DoubleJump изначально скрыт.");
        }
        else
        {
            Debug.LogError("doubleJumpTextObject не назначен в инспекторе!");
        }

        UpdateSkillPointsText(); // Обновляем текст очков

        // Настраиваем слушателей для кнопок
        SetupButtonListeners();
        Debug.Log("Скрипт инициализирован. Очков: " + skillPoints);
    }

    void Update()
    {
        // Открытие/закрытие SkillTree по клавише T
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleSkillTreePanel();
        }
    }

    // Переключение панели навыков с управлением паузой
    public void ToggleSkillTreePanel()
    {
        if (SkillTreePanel != null)
        {
            isMenuOpen = !isMenuOpen;
            SkillTreePanel.SetActive(isMenuOpen);
            Debug.Log("Панель SkillTreePanel " + (isMenuOpen ? "открыта" : "закрыта"));

            // Управление паузой игры
            if (isMenuOpen)
            {
                Time.timeScale = 0f; // Пауза игры
            }
            else
            {
                Time.timeScale = 1f; // Возобновление игры
            }

            UIStateManager.Instance.SetMenuState(isMenuOpen, "SkillTree");
            UpdateSkillPointsText();
        }
        else
        {
            Debug.LogError("SkillTreePanel не назначен для переключения!");
        }
    }

    // Настройка слушателей для кнопок
    private void SetupButtonListeners()
    {
        if (sprintButton != null)
        {
            sprintButtonComponent = sprintButton.GetComponent<Button>();
            if (sprintButtonComponent != null)
            {
                sprintButtonComponent.onClick.AddListener(OnSprintButtonClicked);
                Debug.Log("Слушатель для кнопки Sprint добавлен.");
            }
            else
            {
                Debug.LogWarning("Компонент Button не найден на sprintButton!");
            }
        }
        else
        {
            Debug.LogWarning("sprintButton не назначен в инспекторе!");
        }

        if (doubleJumpButton != null)
        {
            doubleJumpButtonComponent = doubleJumpButton.GetComponent<Button>();
            if (doubleJumpButtonComponent != null)
            {
                doubleJumpButtonComponent.onClick.AddListener(OnDoubleJumpButtonClicked);
                Debug.Log("Слушатель для кнопки DoubleJump добавлен.");
            }
            else
            {
                Debug.LogWarning("Компонент Button не найден на doubleJumpButton!");
            }
        }
        else
        {
            Debug.LogWarning("doubleJumpButton не назначен в инспекторе!");
        }
    }

    // Обработка нажатия на кнопку Sprint
    private void OnSprintButtonClicked()
    {
        Debug.Log("Кнопка Sprint нажата!");
        if (!isSprintUnlocked && UseSkillPoint())
        {
            isSprintUnlocked = true;
            Debug.Log("Навык Sprint разблокирован! Осталось очков: " + skillPoints);
            CheckUnlockDependencies();
        }
        else
        {
            Debug.Log("Не удалось разблокировать Sprint (недостаточно очков или уже разблокировано).");
        }
    }

    // Обработка нажатия на кнопку DoubleJump
    private void OnDoubleJumpButtonClicked()
    {
        Debug.Log("Кнопка DoubleJump нажата!");
        if (!isDoubleJumpUnlocked && doubleJumpLock != null && doubleJumpLock.activeSelf)
        {
            Debug.Log("Замок активен. Запускаем анимацию дрожания...");
            if (lockRectTransform != null && !isShaking) // Проверяем, не идет ли уже анимация
            {
                StartCoroutine(ShakeLock()); // Запускаем корутину для анимации
            }
            else if (isShaking)
            {
                Debug.Log("Анимация уже идет, повторный запуск невозможен.");
            }
            else
            {
                Debug.LogError("lockRectTransform не найден!");
            }
            Debug.Log("Замок на DoubleJump ещё активен! Разблокируйте предусловия.");
        }
        else if (!isDoubleJumpUnlocked && !doubleJumpLock.activeSelf && UseSkillPoint())
        {
            isDoubleJumpUnlocked = true;
            Debug.Log("Навык DoubleJump разблокирован! Осталось очков: " + skillPoints);
        }
        else
        {
            if (isDoubleJumpUnlocked)
            {
                Debug.Log("DoubleJump уже разблокирован!");
            }
            else if (skillPoints <= 0)
            {
                Debug.Log("Недостаточно очков навыков!");
            }
        }
    }

    // Корутина для анимации дрожания с использованием Time.unscaledDeltaTime
    private IEnumerator ShakeLock()
    {
        isShaking = true; // Устанавливаем флаг, что анимация идет
        Vector2 originalPosition = lockRectTransform.anchoredPosition;
        float shakeDuration = 0.3f; // Длительность анимации (0.3 секунды)
        float elapsedTime = 0f;
        float shakeMagnitude = 10f; // Амплитуда дрожания

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Используем реальное время
            float xOffset = Random.Range(-shakeMagnitude, shakeMagnitude); // Случайное смещение
            lockRectTransform.anchoredPosition = new Vector2(originalPosition.x + xOffset, originalPosition.y);
            yield return null; // Ждем следующий кадр
        }

        // Явно возвращаем замок в исходное положение после завершения
        lockRectTransform.anchoredPosition = originalPosition;
        isShaking = false; // Сбрасываем флаг
        Debug.Log("Анимация дрожания завершена через 0.3 секунды.");
    }

    // Использование очка навыка
    public bool UseSkillPoint()
    {
        if (skillPoints > 0)
        {
            skillPoints--;
            UpdateSkillPointsText();
            Debug.Log("Очко использовано. Осталось: " + skillPoints);
            return true;
        }
        Debug.Log("Недостаточно очков навыков!");
        return false;
    }

    // Получение текущего количества очков
    public int GetSkillPoints()
    {
        return skillPoints;
    }

    // Обновление текста очков навыков
    private void UpdateSkillPointsText()
    {
        if (skillPointsText != null)
        {
            skillPointsText.text = $"Очки навыков: {skillPoints}";
            Debug.Log("Текст очков навыков обновлен: " + skillPointsText.text);
        }
        else
        {
            Debug.LogWarning("skillPointsText не назначен в инспекторе!");
        }
    }

    // Проверка зависимостей для разблокировки
    private void CheckUnlockDependencies()
    {
        if (isSprintUnlocked && !isDoubleJumpUnlocked && doubleJumpLock != null)
        {
            doubleJumpLock.SetActive(false); // Убираем замок
            Debug.Log("Замок doubleJumpLock деактивирован.");
            if (doubleJumpButtonComponent != null)
            {
                doubleJumpButtonComponent.interactable = true; // Делаем кнопку кликабельной
                Debug.Log("Кнопка DoubleJump теперь кликабельна.");
            }
            if (doubleJumpTextObject != null)
            {
                doubleJumpTextObject.SetActive(true); // Показываем текст после снятия замка
                Debug.Log("Текст DoubleJump показан после снятия замка.");
            }
            Debug.Log("Замок с DoubleJump убран, кнопка и текст активны!");
        }
    }

    // Очистка слушателей при уничтожении объекта
    void OnDestroy()
    {
        if (sprintButtonComponent != null)
        {
            sprintButtonComponent.onClick.RemoveListener(OnSprintButtonClicked);
            Debug.Log("Слушатель для кнопки Sprint удален.");
        }
        if (doubleJumpButtonComponent != null)
        {
            doubleJumpButtonComponent.onClick.RemoveListener(OnDoubleJumpButtonClicked);
            Debug.Log("Слушатель для кнопки DoubleJump удален.");
        }
    }
}