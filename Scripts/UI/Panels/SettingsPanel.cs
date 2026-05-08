//
// Settings panel that provides Resume and Exit buttons, plus sub-panels for
// general options (volume) and mouse sensitivity (camera, map, skill tree).
//

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : BasePanel
{
    [InjectAttribute1] private IUIManager uiManager { get; set; }

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button generalButton;
    [SerializeField] private Button controlsButton;

    [Header("General settings")]
    [SerializeField] private GameObject generalSubPanel;
    [SerializeField] private GameObject controlsSubPanel;
    [SerializeField] private Slider volumeSlider;

    [Header("Mouse sensitivity (DPI)")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider mapDragDpiSlider;
    [SerializeField] private Slider skillTreeDragDpiSlider;

    // Injects dependencies, wires all buttons, and initialises all sliders from saved preferences
    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);

        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        if (generalButton != null) generalButton.onClick.AddListener(OnGeneralClicked);
        if (controlsButton != null) controlsButton.onClick.AddListener(OnControlsClicked);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 1;
            volumeSlider.maxValue = 10;
            volumeSlider.wholeNumbers = true;
            volumeSlider.value = AudioListener.volume > 0 ? AudioListener.volume * 10f : 10f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0.1f;
            mouseSensitivitySlider.maxValue = 5f;
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
            mouseSensitivitySlider.onValueChanged.AddListener(val => PlayerPrefs.SetFloat("MouseSensitivity", val));
        }

        if (mapDragDpiSlider != null)
        {
            mapDragDpiSlider.minValue = 0.1f;
            mapDragDpiSlider.maxValue = 5f;
            mapDragDpiSlider.value = PlayerPrefs.GetFloat("MapDragDPI", 1f);
            mapDragDpiSlider.onValueChanged.AddListener(val => PlayerPrefs.SetFloat("MapDragDPI", val));
        }

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

    // Closes the current panel through the UI manager
    private void OnResumeClicked() { if (uiManager != null) uiManager.CloseCurrentPanel(); }

    // Quits the application (or stops Play mode in the editor)
    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Toggles the General sub-panel open/closed, closing any other open sub-panel first
    private void OnGeneralClicked()
    {
        bool isOpening = generalSubPanel != null && !generalSubPanel.activeSelf;
        if (uiManager != null) uiManager.CloseAllChildren(UIManager.PanelType.Settings);
        else if (controlsSubPanel != null) controlsSubPanel.SetActive(false);
        if (isOpening && generalSubPanel != null) generalSubPanel.SetActive(true);
    }

    // Toggles the Controls sub-panel open/closed, closing any other open sub-panel first
    private void OnControlsClicked()
    {
        bool isOpening = controlsSubPanel != null && !controlsSubPanel.activeSelf;
        if (uiManager != null) uiManager.CloseAllChildren(UIManager.PanelType.Settings);
        else if (generalSubPanel != null) generalSubPanel.SetActive(false);
        if (isOpening && controlsSubPanel != null) controlsSubPanel.SetActive(true);
    }

    // Maps the 1-10 slider value to Unity's AudioListener volume (0-1 range)
    private void OnVolumeChanged(float value) { AudioListener.volume = value / 10f; }
}
