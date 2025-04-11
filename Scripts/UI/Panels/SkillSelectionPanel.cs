using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillPanelUI : BasePanel, ISkillPanelUI
{
    [SerializeField] private TextMeshProUGUI skillsButtonText;
    [SerializeField] private Button skillsButton;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;
    [InjectAttribute1] private ISkillPanelManager skillPanelManager { get; set; }
    [InjectAttribute1] private IUIManager uiManager { get; set; }

    private GameObject panelSelection;

    // Метод инициализации, вызываемый менеджером
    public void Initialize(ISkillPanelManager manager)
    {
        skillPanelManager = manager;
        DependencyContainer1.InjectDependencies(this);

        panelSelection = uiManager.GetPanel(UIManager.PanelType.Selection);
        if (panelSelection == null)
        {
            Debug.LogError("Selection panel not found in UIManager!");
        }

        SetupSkillsButton();
        CreatePanelButtons();
    }

    private void SetupSkillsButton()
    {
        if (skillsButton != null)
        {
            skillsButton.onClick.RemoveAllListeners();
            skillsButton.onClick.AddListener(TogglePanelSelection);
        }
        else
        {
            Debug.LogError("SkillsButton is not assigned in the inspector!");
        }
    }

    private void TogglePanelSelection()
    {
        if (panelSelection != null)
        {
            panelSelection.SetActive(!panelSelection.activeSelf);
            UpdateSkillsButtonText();
        }
    }

    private void CreatePanelButtons()
    {
        if (buttonContainer == null || buttonPrefab == null)
        {
            Debug.LogError("Button container or prefab not assigned!");
            return;
        }

        foreach (var panel in skillPanelManager.GetPanels())
        {
            if (panel.isSelectable) // Создаем кнопку только если панель доступна для выбора
            {
                Button button = Instantiate(buttonPrefab, buttonContainer);
                button.GetComponentInChildren<TextMeshProUGUI>().text = panel.panelName;
                button.onClick.AddListener(() => 
                {
                    skillPanelManager.SwitchToPanel(panel);
                    if (panelSelection != null)
                    {
                        Close();
                    }
                });
            }
        }
    }

    public void UpdateSkillsButtonText()
    {
        if (skillsButtonText != null)
        {
            skillsButtonText.text = panelSelection != null && panelSelection.activeSelf 
                ? "Selection" 
                : skillPanelManager.GetCurrentPanelName();
        }
    }

    public void UnlockPanel(string panelName)
    {
        var panelList = skillPanelManager.GetPanels();
        var panel = panelList.Find(p => p.panelName == panelName);
        if (panel != null)
        {
            panel.isSelectable = true;
            // Обновляем кнопки в UI
            ClearPanelButtons(); // Метод для очистки старых кнопок
            CreatePanelButtons(); // Пересоздаем кнопки с учетом нового состояния
        }
    }

    // Пример метода очистки кнопок
    private void ClearPanelButtons()
    {
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public override void Close()
    {
        base.Close();
        UpdateSkillsButtonText();
    }
}