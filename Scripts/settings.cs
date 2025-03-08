using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsMenu;
    private bool isSettingsOpen = false;
    public bool IsSettingsOpen => isSettingsOpen;
    public Button quitButton;
    public Button playButton;
    public Button generalButton;
    public Button backButton;
    public ThirdPersonCamera cameraController;
    public InventoryManager inventoryManager;
    public skilltree skillTree;

    public Slider mouseSensitivitySlider;
    public TMP_Text mouseSensitivityText;
    public TMP_InputField sensitivityInputField;

    private bool isEditing = false;
    private float lastClickTime = 0f;
    private float doubleClickTime = 0.3f;

    private ColorBlock defaultColors;
    private bool isGeneralWindowOpen = false;

    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    private bool wasInventoryOpenLastFrame = false;

    void Start()
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(Play);
        }

        if (generalButton != null)
        {
            generalButton.onClick.AddListener(ShowGeneralSettings);
            defaultColors = generalButton.colors;
        }

        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
            backButton.onClick.AddListener(HideGeneralSettings);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.gameObject.SetActive(false);
        }
        if (mouseSensitivityText != null)
        {
            mouseSensitivityText.gameObject.SetActive(false);
        }
        if (sensitivityInputField != null)
        {
            sensitivityInputField.gameObject.SetActive(false);
        }

        if (mouseSensitivitySlider != null && cameraController != null)
        {
            mouseSensitivitySlider.value = cameraController.mouseSensitivity;
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        }

        if (sensitivityInputField != null)
        {
            sensitivityInputField.onEndEdit.AddListener(OnEndEdit);
        }

        if (inventoryManager == null)
        {
            inventoryManager = Object.FindFirstObjectByType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogError("InventoryManager не найден в сцене!");
            }
        }

        if (skillTree == null)
        {
            skillTree = Object.FindFirstObjectByType<skilltree>();
            if (skillTree == null)
            {
                Debug.LogError("skilltree не найден в сцене!");
            }
        }

        SetupTextClickHandler();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isSettingsOpen)
            {
                if (backButton != null && backButton.gameObject.activeSelf)
                {
                    HideGeneralSettings();
                }
                else
                {
                    ToggleSettingsMenu();
                }
            }
            else if (skillTree != null && skillTree.isMenuOpen)
            {
                skillTree.ToggleSkillTreePanel();
            }
            else if (inventoryManager != null && inventoryManager.IsInventoryOpen)
            {
                inventoryManager.ToggleInventory();
            }
            else if (inventoryManager != null && skillTree != null)
            {
                if (!inventoryManager.IsInventoryOpen && !skillTree.isMenuOpen && !wasInventoryOpenLastFrame)
                {
                    ToggleSettingsMenu();
                }
            }
            else if (skillTree != null && !skillTree.isMenuOpen)
            {
                ToggleSettingsMenu();
            }
        }

        if (Input.GetKeyDown(KeyCode.U) && skillTree != null)
        {
            if (!isSettingsOpen && (inventoryManager == null || !inventoryManager.IsInventoryOpen))
            {
                skillTree.ToggleSkillTreePanel();
            }
        }

        if ((Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)) && inventoryManager != null)
        {
            if (!isSettingsOpen && (skillTree == null || !skillTree.isMenuOpen))
            {
                inventoryManager.ToggleInventory();
            }
        }

        if (inventoryManager != null)
        {
            wasInventoryOpenLastFrame = inventoryManager.IsInventoryOpen;
        }
    }

    public void ToggleSettingsMenu()
    {
        isSettingsOpen = !isSettingsOpen;
        UpdateGameState();
    }

    void UpdateGameState()
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(isSettingsOpen);
        }
        UIStateManager.Instance.SetMenuState(isSettingsOpen, "Settings");
        if (cameraController != null)
        {
            cameraController.isSettingsOpen = isSettingsOpen;
        }
    }

    void Play()
    {
        HideGeneralSettings();
        ToggleSettingsMenu();
        Debug.Log("Продолжаем");
    }

    void QuitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void Awake()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void SetMouseSensitivity(float sensitivity)
    {
        if (cameraController != null)
        {
            cameraController.mouseSensitivity = sensitivity;
            UpdateMouseSensitivityText(sensitivity);
        }
    }

    private void UpdateMouseSensitivityText(float value)
    {
        if (mouseSensitivityText != null)
        {
            mouseSensitivityText.text = $"{value:F1}";
        }
    }

    private void SetupTextClickHandler()
    {
        if (mouseSensitivityText == null) return;

        EventTrigger trigger = mouseSensitivityText.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = mouseSensitivityText.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener((data) => { OnTextClicked((PointerEventData)data); });
        trigger.triggers.Add(entry);
    }

    private void OnTextClicked(PointerEventData eventData)
    {
        if (!isSettingsOpen || isEditing) return;

        float timeSinceLastClick = Time.unscaledTime - lastClickTime;
        if (timeSinceLastClick <= doubleClickTime)
        {
            StartEditing();
        }
        lastClickTime = Time.unscaledTime;
    }

    private void StartEditing()
    {
        isEditing = true;
        if (sensitivityInputField != null)
        {
            sensitivityInputField.gameObject.SetActive(true);
            sensitivityInputField.text = cameraController.mouseSensitivity.ToString();
            sensitivityInputField.ActivateInputField();
        }
        if (mouseSensitivityText != null)
        {
            mouseSensitivityText.gameObject.SetActive(false);
        }
    }

    private void OnEndEdit(string value)
    {
        isEditing = false;
        if (float.TryParse(value, out float sensitivity))
        {
            sensitivity = Mathf.Clamp(sensitivity, mouseSensitivitySlider.minValue, mouseSensitivitySlider.maxValue);
            if (cameraController != null)
            {
                cameraController.mouseSensitivity = sensitivity;
                mouseSensitivitySlider.value = sensitivity;
                UpdateMouseSensitivityText(sensitivity);
            }
        }
        sensitivityInputField.gameObject.SetActive(false);
        if (mouseSensitivityText != null)
        {
            mouseSensitivityText.gameObject.SetActive(true);
        }
    }

    private void ShowGeneralSettings()
    {
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.gameObject.SetActive(true);
            mouseSensitivitySlider.value = cameraController.mouseSensitivity;
        }
        if (mouseSensitivityText != null)
        {
            mouseSensitivityText.gameObject.SetActive(true);
            UpdateMouseSensitivityText(cameraController.mouseSensitivity);
        }
        if (backButton != null)
        {
            backButton.gameObject.SetActive(true);
        }
        
        isGeneralWindowOpen = true;
        UpdateButtonColor();
    }

    private void HideGeneralSettings()
    {
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.gameObject.SetActive(false);
        }
        if (mouseSensitivityText != null)
        {
            mouseSensitivityText.gameObject.SetActive(false);
        }
        if (sensitivityInputField != null)
        {
            sensitivityInputField.gameObject.SetActive(false);
        }
        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
        }
        
        isGeneralWindowOpen = false;
        UpdateButtonColor();
    }

    private void UpdateButtonColor()
    {
        if (generalButton == null) return;

        ColorBlock colors = generalButton.colors;
        
        if (isGeneralWindowOpen)
        {
            colors.normalColor = new Color(0.2f, 0.6f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.7f, 1f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.9f);
            colors.selectedColor = new Color(0.2f, 0.6f, 1f);
        }
        else
        {
            colors = defaultColors;
        }
        
        generalButton.colors = colors;
    }
}