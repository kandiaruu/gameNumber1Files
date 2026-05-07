using UnityEngine;
using UnityEngine.UI;

public class DungeonEntryPanel : BasePanel, IDungeonEntryPanel
{
    [InjectAttribute1] private IDungeonFloorManager floorManager { get; set; }
    [InjectAttribute1] private IUIManager uiManager { get; set; }

    [Header("Containers")]
    public GameObject rankSelectionContainer; // Кнопки S, A, B...
    public GameObject insideDungeonContainer; // Выход в мир / Удалить (внутри данжа)
    public GameObject outsideActiveContainer; // Вернуться / Удалить (снаружи)

    [Header("Rank Buttons")]
    public Button btnRankF; // 1
    public Button btnRankE; // 2
    public Button btnRankD; // 4
    public Button btnRankC; // 8
    public Button btnRankB; // 16
    public Button btnRankA; // 32
    public Button btnRankS; // 64

    [Header("Inside Buttons")]
    public Button btnReturnToWorld;
    public Button btnReturnAndDelete;

    [Header("Outside Active Buttons")]
    public Button btnResumeDungeon;
    public Button btnDeleteDungeonOutside;

    // ИЗМЕНЕНИЕ ЗДЕСЬ: используем Awake вместо Start
    public override void Awake()
    {
        base.Awake(); // Обязательно вызываем базовый метод из BasePanel
        
        DependencyContainer1.InjectDependencies(this);

        // Биндим кнопки рангов (проверяем на null на всякий случай)
        if (btnRankF != null) btnRankF.onClick.AddListener(() => OnRankSelected(1));
        if (btnRankE != null) btnRankE.onClick.AddListener(() => OnRankSelected(2));
        if (btnRankD != null) btnRankD.onClick.AddListener(() => OnRankSelected(4));
        if (btnRankC != null) btnRankC.onClick.AddListener(() => OnRankSelected(8));
        if (btnRankB != null) btnRankB.onClick.AddListener(() => OnRankSelected(16));
        if (btnRankA != null) btnRankA.onClick.AddListener(() => OnRankSelected(32));
        if (btnRankS != null) btnRankS.onClick.AddListener(() => OnRankSelected(64));

        // Биндим остальные кнопки
        if (btnReturnToWorld != null) btnReturnToWorld.onClick.AddListener(OnReturnToWorld);
        if (btnReturnAndDelete != null) btnReturnAndDelete.onClick.AddListener(OnReturnAndDelete);
        if (btnResumeDungeon != null) btnResumeDungeon.onClick.AddListener(OnResumeDungeon);
        if (btnDeleteDungeonOutside != null) btnDeleteDungeonOutside.onClick.AddListener(OnDeleteDungeonOutside);
    }

    public override void Open()
    {
        base.Open();
        UpdateUIState();
    }

    private void UpdateUIState()
    {
        if (rankSelectionContainer != null) rankSelectionContainer.SetActive(false);
        if (insideDungeonContainer != null) insideDungeonContainer.SetActive(false);
        if (outsideActiveContainer != null) outsideActiveContainer.SetActive(false);

        // Теперь floorManager точно не null
        if (floorManager != null && floorManager.IsInsideDungeon)
        {
            if (insideDungeonContainer != null) insideDungeonContainer.SetActive(true);
        }
        else if (floorManager != null && floorManager.HasActiveDungeon)
        {
            if (outsideActiveContainer != null) outsideActiveContainer.SetActive(true);
        }
        else
        {
            if (rankSelectionContainer != null) rankSelectionContainer.SetActive(true);
        }
    }

    private void OnRankSelected(int floors)
    {
        if (uiManager != null) uiManager.CloseCurrentPanel();
        if (floorManager != null) floorManager.StartNewDungeon(floors);
    }

    private void OnReturnToWorld()
    {
        if (uiManager != null) uiManager.CloseCurrentPanel();
        if (floorManager != null) floorManager.ExitDungeonToWorld();
    }

    private void OnReturnAndDelete()
    {
        if (uiManager != null) uiManager.CloseCurrentPanel();
        if (floorManager != null) floorManager.ExitAndDeleteDungeon();
    }

    private void OnResumeDungeon()
    {
        if (uiManager != null) uiManager.CloseCurrentPanel();
        if (floorManager != null) floorManager.ResumeDungeon();
    }

    private void OnDeleteDungeonOutside()
    {
        if (floorManager != null) floorManager.DeleteDungeonFromWorld();
        UpdateUIState(); // Обновляем UI, чтобы показался выбор рангов
    }
}