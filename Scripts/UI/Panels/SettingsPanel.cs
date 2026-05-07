using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : BasePanel
{
    [InjectAttribute1] private IUIManager uiManager { get; set; }

    [Header("Кнопки")]
    [SerializeField] private Button resumeButton; 
    [SerializeField] private Button exitButton;   
    [SerializeField] private Button generalButton; 
    [SerializeField] private Button controlsButton; 

    [Header("Общие настройки (General)")]
    [SerializeField] private GameObject generalSubPanel; 
    [SerializeField] private GameObject controlsSubPanel; 
    [SerializeField] private Slider volumeSlider;        
    
    [Header("Настройки мыши (DPI)")]
    [SerializeField] private Slider mouseSensitivitySlider; // <--- ДОБАВЛЕНО: Чувствительность в игре
    [SerializeField] private Slider mapDragDpiSlider;       
    [SerializeField] private Slider skillTreeDragDpiSlider; 

    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);

        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        if (generalButton != null) generalButton.onClick.AddListener(OnGeneralClicked);
        if (controlsButton != null) controlsButton.onClick.AddListener(OnControlsClicked);

        // Настройка громкости
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 1;
            volumeSlider.maxValue = 10;
            volumeSlider.wholeNumbers = true;
            volumeSlider.value = AudioListener.volume > 0 ? AudioListener.volume * 10f : 10f; 
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        
        // <--- Настройка ползунков DPI --->
        
        // 1. Чувствительность камеры (игрока)
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0.1f;
            mouseSensitivitySlider.maxValue = 5f; 
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f); 
            mouseSensitivitySlider.onValueChanged.AddListener(val => PlayerPrefs.SetFloat("MouseSensitivity", val));
        }

        // 2. Чувствительность карты
        if (mapDragDpiSlider != null)
        {
            mapDragDpiSlider.minValue = 0.1f;
            mapDragDpiSlider.maxValue = 5f; 
            mapDragDpiSlider.value = PlayerPrefs.GetFloat("MapDragDPI", 1f); 
            mapDragDpiSlider.onValueChanged.AddListener(val => PlayerPrefs.SetFloat("MapDragDPI", val));
        }

        // 3. Чувствительность дерева навыков
        if (skillTreeDragDpiSlider != null)
        {
            skillTreeDragDpiSlider.minValue = 0.1f;
            skillTreeDragDpiSlider.maxValue = 5f;
            skillTreeDragDpiSlider.value = PlayerPrefs.GetFloat("SkillTreeDragDPI", 1f); 
            skillTreeDragDpiSlider.onValueChanged.AddListener(val => PlayerPrefs.SetFloat("SkillTreeDragDPI", val));
        }
        
        if (generalSubPanel != null) generalSubPanel.SetActive(false);
        if (controlsSubPanel != null) controlsSubPanel.SetActive(false);
    }

    private void OnResumeClicked() { if (uiManager != null) uiManager.CloseCurrentPanel(); }
    private void OnExitClicked()
    {

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    private void OnGeneralClicked()
    {
        bool isOpening = generalSubPanel != null && !generalSubPanel.activeSelf;
        if (uiManager != null) uiManager.CloseAllChildren(UIManager.PanelType.Settings);
        else if (controlsSubPanel != null) controlsSubPanel.SetActive(false);
        if (isOpening && generalSubPanel != null) generalSubPanel.SetActive(true);
    }
    private void OnControlsClicked()
    {
        bool isOpening = controlsSubPanel != null && !controlsSubPanel.activeSelf;
        if (uiManager != null) uiManager.CloseAllChildren(UIManager.PanelType.Settings);
        else if (generalSubPanel != null) generalSubPanel.SetActive(false);
        if (isOpening && controlsSubPanel != null) controlsSubPanel.SetActive(true);
    }
    private void OnVolumeChanged(float value) { AudioListener.volume = value / 10f; }
}