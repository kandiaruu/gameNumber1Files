using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    public static UIStateManager Instance { get; private set; }
    private bool isAnyMenuOpen = false;
    private bool isInventoryOpen = false;
    private bool isSettingsOpen = false;
    private bool isSkillTreeOpen = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        isAnyMenuOpen = false;
        isInventoryOpen = false;
        isSettingsOpen = false;
        isSkillTreeOpen = false;
        UpdateGameState();
    }

    public void SetMenuState(bool isOpen, string menuType)
    {
        switch (menuType)
        {
            case "Inventory":
                isInventoryOpen = isOpen;
                break;
            case "Settings":
                isSettingsOpen = isOpen;
                break;
            case "SkillTree":
                isSkillTreeOpen = isOpen;
                break;
            default:
                Debug.LogWarning($"Неизвестный тип меню: {menuType}");
                return;
        }
        isAnyMenuOpen = isInventoryOpen || isSettingsOpen || isSkillTreeOpen;
        UpdateGameState();
    }

    public bool IsAnyMenuOpen()
    {
        return isAnyMenuOpen;
    }

    private void UpdateGameState()
    {
        if (isSettingsOpen || isSkillTreeOpen)
        {
            Time.timeScale = 0f; // Остановка игры
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (isInventoryOpen)
        {
            Time.timeScale = 1f; // Игра продолжается
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            Time.timeScale = 1f; // Игра продолжается
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        Debug.Log($"Состояние обновлено: AnyMenu = {isAnyMenuOpen}, Inventory = {isInventoryOpen}, Settings = {isSettingsOpen}, SkillTree = {isSkillTreeOpen}, TimeScale = {Time.timeScale}");
    }
}