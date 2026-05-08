//
// Panel for entering, resuming, or deleting a dungeon. Shows different container
// groups depending on whether the player is inside a dungeon, has an active dungeon
// waiting outside, or has no dungeon at all.
//

using UnityEngine;
using UnityEngine.UI;

public class DungeonEntryPanel : BasePanel, IDungeonEntryPanel
{
    [InjectAttribute1] private IDungeonFloorManager floorManager { get; set; }
    [InjectAttribute1] private IUIManager uiManager { get; set; }

    [Header("Containers")]
    public GameObject rankSelectionContainer;
    public GameObject insideDungeonContainer;
    public GameObject outsideActiveContainer;

    [Header("Rank Buttons")]
    public Button btnRankF;
    public Button btnRankE;
    public Button btnRankD;
    public Button btnRankC;
    public Button btnRankB;
    public Button btnRankA;
    public Button btnRankS;

    [Header("Inside Buttons")]
    public Button btnReturnToWorld;
    public Button btnReturnAndDelete;

    [Header("Outside Active Buttons")]
    public Button btnResumeDungeon;
    public Button btnDeleteDungeonOutside;

    // Injects dependencies and wires all rank and action buttons
    public override void Awake()
    {
        base.Awake();

        DependencyContainer1.InjectDependencies(this);

        if (btnRankF != null) btnRankF.onClick.AddListener(() => OnRankSelected(1));
        if (btnRankE != null) btnRankE.onClick.AddListener(() => OnRankSelected(2));
        if (btnRankD != null) btnRankD.onClick.AddListener(() => OnRankSelected(4));
        if (btnRankC != null) btnRankC.onClick.AddListener(() => OnRankSelected(8));
        if (btnRankB != null) btnRankB.onClick.AddListener(() => OnRankSelected(16));
        if (btnRankA != null) btnRankA.onClick.AddListener(() => OnRankSelected(32));
        if (btnRankS != null) btnRankS.onClick.AddListener(() => OnRankSelected(64));

        if (btnReturnToWorld != null) btnReturnToWorld.onClick.AddListener(OnReturnToWorld);
        if (btnReturnAndDelete != null) btnReturnAndDelete.onClick.AddListener(OnReturnAndDelete);
        if (btnResumeDungeon != null) btnResumeDungeon.onClick.AddListener(OnResumeDungeon);
        if (btnDeleteDungeonOutside != null) btnDeleteDungeonOutside.onClick.AddListener(OnDeleteDungeonOutside);
    }

    // Opens the panel and refreshes which container group is visible
    public override void Open()
    {
        base.Open();
        UpdateUIState();
    }

    // Shows the correct container group based on the player's current dungeon state
    private void UpdateUIState()
    {
        if (rankSelectionContainer != null) rankSelectionContainer.SetActive(false);
        if (insideDungeonContainer != null) insideDungeonContainer.SetActive(false);
        if (outsideActiveContainer != null) outsideActiveContainer.SetActive(false);

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

    // Closes the UI and starts a new dungeon with the given number of floors
    private void OnRankSelected(int floors)
    {
        if (uiManager != null) uiManager.CloseCurrentPanel();
        if (floorManager != null) floorManager.StartNewDungeon(floors);
    }

    // Closes the UI and exits the dungeon, returning the player to the world
    private void OnReturnToWorld()
    {
        if (uiManager != null) uiManager.CloseCurrentPanel();
        if (floorManager != null) floorManager.ExitDungeonToWorld();
    }

    // Closes the UI, exits the dungeon, and permanently deletes it
    private void OnReturnAndDelete()
    {
        if (uiManager != null) uiManager.CloseCurrentPanel();
        if (floorManager != null) floorManager.ExitAndDeleteDungeon();
    }

    // Closes the UI and resumes the player's existing dungeon
    private void OnResumeDungeon()
    {
        if (uiManager != null) uiManager.CloseCurrentPanel();
        if (floorManager != null) floorManager.ResumeDungeon();
    }

    // Deletes the dungeon from the world without leaving and refreshes the UI state
    private void OnDeleteDungeonOutside()
    {
        if (floorManager != null) floorManager.DeleteDungeonFromWorld();
        UpdateUIState();
    }
}
