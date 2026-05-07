using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsControlsPanel : BasePanel
{
    [InjectAttribute1] private IUIManager uiManager { get; set; }
    [InjectAttribute1] private ISkillEquipManager equipManager { get; set; }

    [Header("Бинды Навыков (6 штук)")]
    [SerializeField] private Button[] skillRebindButtons; 
    [SerializeField] private TextMeshProUGUI[] skillKeyTexts; 

    [Header("Бинды Панелей (5 штук)")]
    [SerializeField] private Button[] panelRebindButtons; 
    [SerializeField] private TextMeshProUGUI[] panelKeyTexts; 
    private int waitingForSkillSlot = -1; 
    private int waitingForPanelSlot = -1; 

    // Порядок панелей: 0-SkillTree, 1-Status, 2-Inventory, 3-Map, 4-SkillSelect
    private UIManager.PanelType[] panelTypesToBind = new UIManager.PanelType[]
    {
        UIManager.PanelType.SkillTree,
        UIManager.PanelType.Status,
        UIManager.PanelType.Inventory3, 
        UIManager.PanelType.Map,
        UIManager.PanelType.SkillSelect
    };

    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);

        // Подписываем 6 кнопок навыков
        for (int i = 0; i < skillRebindButtons.Length; i++)
        {
            int index = i; 
            if (skillRebindButtons[i] != null) skillRebindButtons[i].onClick.AddListener(() => StartRebindSkill(index));
        }

        // Подписываем 5 кнопок UI панелей
        for (int i = 0; i < panelRebindButtons.Length; i++)
        {
            int index = i; 
            if (panelRebindButtons[i] != null) panelRebindButtons[i].onClick.AddListener(() => StartRebindPanel(index));
        }
    }

    private void OnEnable()
    {
        waitingForSkillSlot = -1;
        waitingForPanelSlot = -1;
        RefreshUI();
    }

    private void RefreshUI()
    {
        // Обновляем тексты навыков
        if (equipManager != null && skillKeyTexts != null)
        {
            for (int i = 0; i < skillKeyTexts.Length; i++)
            {
                if (skillKeyTexts[i] != null && i < equipManager.ActiveSkillKeys.Length)
                    skillKeyTexts[i].text = equipManager.ActiveSkillKeys[i].ToString();
            }
        }

        // Обновляем тексты панелей
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

    private void StartRebindSkill(int slotIndex)
    {
        waitingForSkillSlot = slotIndex;
        waitingForPanelSlot = -1; // Сбрасываем ожидание панели
        if (skillKeyTexts[slotIndex] != null) skillKeyTexts[slotIndex].text = "...";
    }

    private void StartRebindPanel(int slotIndex)
    {
        waitingForPanelSlot = slotIndex;
        waitingForSkillSlot = -1; // Сбрасываем ожидание навыка
        if (panelKeyTexts[slotIndex] != null) panelKeyTexts[slotIndex].text = "...";
    }

    private void Update()
    {
        // Если мы ждем нажатия любой кнопки
        if ((waitingForSkillSlot != -1 || waitingForPanelSlot != -1) && Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode == KeyCode.Mouse0) return; // Игнорируем ЛКМ

                    if (keyCode == KeyCode.Escape)
                    {
                        waitingForSkillSlot = -1;
                        waitingForPanelSlot = -1;
                        RefreshUI();
                        return;
                    }

                    // Если биндим НАВЫК
                    if (waitingForSkillSlot != -1 && equipManager != null)
                    {
                        equipManager.SetKey(waitingForSkillSlot, keyCode);
                        waitingForSkillSlot = -1;
                        
                        var skillPanel = FindFirstObjectByType<SkillSelectPanel>();
                        if (skillPanel != null && skillPanel.gameObject.activeInHierarchy) skillPanel.RefreshUI();
                    }
                    // Если биндим ПАНЕЛЬ
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