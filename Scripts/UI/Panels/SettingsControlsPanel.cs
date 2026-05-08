//
// Controls rebinding sub-panel inside Settings. Lets the player reassign
// keybindings for active skill slots and UI panel toggle keys.
//

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsControlsPanel : BasePanel
{
    [InjectAttribute1] private IUIManager uiManager { get; set; }
    [InjectAttribute1] private ISkillEquipManager equipManager { get; set; }

    [Header("Skill binds (6 slots)")]
    [SerializeField] private Button[] skillRebindButtons;
    [SerializeField] private TextMeshProUGUI[] skillKeyTexts;

    [Header("Panel binds (5 slots)")]
    [SerializeField] private Button[] panelRebindButtons;
    [SerializeField] private TextMeshProUGUI[] panelKeyTexts;

    private int waitingForSkillSlot = -1;
    private int waitingForPanelSlot = -1;

    // Panel order: 0-SkillTree, 1-Status, 2-Inventory, 3-Map, 4-SkillSelect
    private UIManager.PanelType[] panelTypesToBind = new UIManager.PanelType[]
    {
        UIManager.PanelType.SkillTree,
        UIManager.PanelType.Status,
        UIManager.PanelType.Inventory3,
        UIManager.PanelType.Map,
        UIManager.PanelType.SkillSelect
    };

    // Injects dependencies and wires up all skill and panel rebind buttons
    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);

        for (int i = 0; i < skillRebindButtons.Length; i++)
        {
            int index = i;
            if (skillRebindButtons[i] != null) skillRebindButtons[i].onClick.AddListener(() => StartRebindSkill(index));
        }

        for (int i = 0; i < panelRebindButtons.Length; i++)
        {
            int index = i;
            if (panelRebindButtons[i] != null) panelRebindButtons[i].onClick.AddListener(() => StartRebindPanel(index));
        }
    }

    // Resets any pending rebind state and refreshes all key labels when the panel is enabled
    private void OnEnable()
    {
        waitingForSkillSlot = -1;
        waitingForPanelSlot = -1;
        RefreshUI();
    }

    // Updates all skill key and panel key text labels from their respective managers
    private void RefreshUI()
    {
        if (equipManager != null && skillKeyTexts != null)
        {
            for (int i = 0; i < skillKeyTexts.Length; i++)
            {
                if (skillKeyTexts[i] != null && i < equipManager.ActiveSkillKeys.Length)
                    skillKeyTexts[i].text = equipManager.ActiveSkillKeys[i].ToString();
            }
        }

        UIManager uiMgr = uiManager as UIManager;
        if (uiMgr != null && panelKeyTexts != null)
        {
            for (int i = 0; i < panelKeyTexts.Length; i++)
            {
                if (panelKeyTexts[i] != null && uiMgr.PanelKeys.ContainsKey(panelTypesToBind[i]))
                {
                    panelKeyTexts[i].text = uiMgr.PanelKeys[panelTypesToBind[i]].ToString();
                }
            }
        }
    }

    // Begins listening for a new key to assign to the given skill slot index
    private void StartRebindSkill(int slotIndex)
    {
        waitingForSkillSlot = slotIndex;
        waitingForPanelSlot = -1;
        if (skillKeyTexts[slotIndex] != null) skillKeyTexts[slotIndex].text = "...";
    }

    // Begins listening for a new key to assign to the given panel slot index
    private void StartRebindPanel(int slotIndex)
    {
        waitingForPanelSlot = slotIndex;
        waitingForSkillSlot = -1;
        if (panelKeyTexts[slotIndex] != null) panelKeyTexts[slotIndex].text = "...";
    }

    // Waits for any key press when a rebind is pending; applies or cancels the rebind accordingly
    private void Update()
    {
        if ((waitingForSkillSlot != -1 || waitingForPanelSlot != -1) && Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode == KeyCode.Mouse0) return;

                    if (keyCode == KeyCode.Escape)
                    {
                        waitingForSkillSlot = -1;
                        waitingForPanelSlot = -1;
                        RefreshUI();
                        return;
                    }

                    if (waitingForSkillSlot != -1 && equipManager != null)
                    {
                        equipManager.SetKey(waitingForSkillSlot, keyCode);
                        waitingForSkillSlot = -1;

                        var skillPanel = FindFirstObjectByType<SkillSelectPanel>();
                        if (skillPanel != null && skillPanel.gameObject.activeInHierarchy) skillPanel.RefreshUI();
                    }
                    else if (waitingForPanelSlot != -1)
                    {
                        UIManager uiMgr = uiManager as UIManager;
                        if (uiMgr != null)
                        {
                            uiMgr.SetPanelKey(panelTypesToBind[waitingForPanelSlot], keyCode);
                        }
                        waitingForPanelSlot = -1;
                    }

                    RefreshUI();
                    break;
                }
            }
        }
    }
}
