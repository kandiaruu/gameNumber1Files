using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillPanelSwitcher : MonoBehaviour, ISkillPanelSwitcher
{
    public enum SkillPanelState
    {
        Normal,
        Hidden
    }

    [SerializeField] private GameObject normalSkillsPanel;
    [SerializeField] private GameObject hiddenSkillsPanel;
    [SerializeField] private TextMeshProUGUI skillsButtonText;
    [SerializeField] private Button skillsButton;
    [InjectAttribute1] private ISkillTreeNavigation skillTreeNavigation { get; set; }
    private SkillPanelState currentSkillState = SkillPanelState.Normal;

    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this); // Добавляем инжекцию зависимостей
        SetupInitialSkillPanels();
        SetupSkillsButton();
    }

    private void SetupInitialSkillPanels()
    {
        if (normalSkillsPanel == null || hiddenSkillsPanel == null)
        {
            Debug.LogError("NormalSkillsPanel или HiddenSkillsPanel не назначены в инспекторе!");
            return;
        }

        if (skillsButtonText == null)
        {
            Debug.LogError("SkillsButtonText не назначен в инспекторе!");
            return;
        }

        if (skillTreeNavigation == null)
        {
            Debug.LogError("SkillTreeNavigation не инициализирован!");
            return;
        }

        normalSkillsPanel.SetActive(true);
        hiddenSkillsPanel.SetActive(false);
        currentSkillState = SkillPanelState.Normal;
        UpdateSkillsButtonText();
        UpdateNavigationGroup(); // Устанавливаем начальную группу
    }

    private void SetupSkillsButton()
    {
        if (skillsButton == null)
        {
            Debug.LogError("SkillsButton не назначен в инспекторе!");
            return;
        }

        skillsButton.onClick.RemoveAllListeners();
        skillsButton.onClick.AddListener(ToggleSkillPanels);
    }

    private void ToggleSkillPanels()
    {
        // Сохраняем текущее состояние перед переключением (если нужно)
        skillTreeNavigation.SavePanelState(currentSkillState);

        if (currentSkillState == SkillPanelState.Normal)
        {
            normalSkillsPanel.SetActive(false);
            hiddenSkillsPanel.SetActive(true);
            currentSkillState = SkillPanelState.Hidden;
        }
        else
        {
            normalSkillsPanel.SetActive(true);
            hiddenSkillsPanel.SetActive(false);
            currentSkillState = SkillPanelState.Normal;
        }

        UpdateSkillsButtonText();
        UpdateNavigationGroup(); // Обновляем группу в SkillTreeNavigation
        skillTreeNavigation.LoadPanelState(currentSkillState); // Загружаем состояние для новой панели
    }

    private void UpdateSkillsButtonText()
    {
        skillsButtonText.text = currentSkillState == SkillPanelState.Normal ? "SKILLS: NORMAL" : "SKILLS: HIDDEN";
    }

    private void UpdateNavigationGroup()
    {
        // Устанавливаем CurrentGroupName в зависимости от текущего состояния
        string groupName = currentSkillState == SkillPanelState.Normal ? "Normal" : "Hidden";
        skillTreeNavigation.SetCurrentGroup(groupName);
        Debug.Log($"Текущая группа установлена: {groupName}");
    }

    public SkillPanelState GetCurrentSkillState()
    {
        return currentSkillState;
    }
}