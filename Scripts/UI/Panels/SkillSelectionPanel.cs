//
// UI panel that lets the player switch between available skill sub-panels.
// Dynamically creates a button for each selectable panel and keeps the header
// button text in sync with the currently active panel name.
//

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

    // Stores the manager reference, injects dependencies, retrieves the selection panel,
    // wires up the skills button, and builds the panel-switch buttons
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

    // Wires the skills header button to the toggle method
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

    // Toggles the visibility of the panel selection overlay and updates the header button label
    public void TogglePanelSelection()
    {
        if (panelSelection != null)
        {
            panelSelection.SetActive(!panelSelection.activeSelf);
            UpdateSkillsButtonText();
        }
    }

    // Instantiates a button for every selectable panel; highlights the currently active one
    public void CreatePanelButtons()
    {
        if (buttonContainer == null || buttonPrefab == null)
        {
            Debug.LogError("Button container or prefab not assigned!");
            return;
        }

        string currentPanelName = skillPanelManager.GetCurrentPanelName();

        foreach (var panel in skillPanelManager.GetPanels())
        {
            if (panel.isSelectable)
            {
                Button button = Instantiate(buttonPrefab, buttonContainer);
                button.GetComponentInChildren<TextMeshProUGUI>().text = panel.panelName;

                if (panel.panelName == currentPanelName)
                {
                    var buttonImage = button.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = new Color32(255, 255, 255, 255);
                    }
                }

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

    // Sets the header button label to either "Selection" (when overlay is open) or the active panel name
    public void UpdateSkillsButtonText()
    {
        if (skillsButtonText != null)
        {
            skillsButtonText.text = panelSelection != null && panelSelection.activeSelf
                ? "Selection"
                : skillPanelManager.GetCurrentPanelName();
        }
    }

    // Marks a panel as selectable, then rebuilds all panel-switch buttons
    public void UnlockPanel(string panelName)
    {
        var panelList = skillPanelManager.GetPanels();
        var panel = panelList.Find(p => p.panelName == panelName);
        if (panel != null)
        {
            panel.isSelectable = true;
            ClearPanelButtons();
            CreatePanelButtons();
        }
    }

    // Destroys all dynamically created panel-switch buttons
    public void ClearPanelButtons()
    {
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    // Closes the panel and syncs the header button text
    public override void Close()
    {
        base.Close();
        UpdateSkillsButtonText();
    }
}
