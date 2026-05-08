//
// DungeonFloorManager orchestrates multi-floor dungeon sessions.
// It tracks the current floor number, manages per-floor GameObject folders,
// shows/hides floors and the overworld, teleports the player to the start room,
// and coordinates the DungeonBuilder and DungeonVisibilityManager.
// Public methods allow entering, advancing, retreating, resuming, and exiting
// a dungeon run, all wrapped in coroutines that display a loading screen.
//

using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class DungeonFloorManager : MonoBehaviour, IDungeonFloorManager
{
    [Header("Dungeon Settings")]
    public int maxFloors = 6;
    [Header("Roots")]
    public Transform dungeonSystemRoot;
    public Transform mapContainerRoot;
    [Header("World/Hub")]
    public GameObject worldRoot;

    [Header("References")]
    public DungeonBuilder builder;
    public Transform player;
    public DungeonVisibilityManager visibilityManager;
    private Dictionary<int, List<NodeInstance>> roomsPerFloor = new Dictionary<int, List<NodeInstance>>();

    [InjectAttribute1] public IUIManager uiManager { get; set; }

    [Header("State")]
    public int currentFloor = 0;
    public bool isInsideDungeon = false;

    private List<GameObject> floorFolders = new List<GameObject>();
    private List<GameObject> mapFolders = new List<GameObject>();
    private Vector3 worldReturnPosition;

    // Injects dependencies and stores the player's initial world position as the return point
    private void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        worldReturnPosition = player.position;
    }

    // Returns true if the player is currently inside the dungeon
    public bool IsInsideDungeon => isInsideDungeon;

    // Returns true if at least one floor has been generated or entered
    public bool HasActiveDungeon => currentFloor > 0 || floorFolders.Count > 0;

    // Toggles between entering the dungeon and exiting to the world depending on the player's current location
    public void OnPortalInteract()
    {
        if (isInsideDungeon)
        {
            ExitToWorld();
        }
        else
        {
            EnterDungeon();
        }
    }

    // Shows the loading screen, disables the current floor, re-enables the world root, and returns the player to their world position
    private void ExitToWorld()
    {
        uiManager.OpenPanel(UIManager.PanelType.Loading);

        isInsideDungeon = false;

        player.position = worldReturnPosition;

        if (worldRoot != null)
        {
            worldRoot.SetActive(true);
        }

        if (currentFloor > 0)
        {
            floorFolders[currentFloor - 1].SetActive(false);
            mapFolders[currentFloor - 1].SetActive(false);
        }

        uiManager.CloseCurrentPanel();
    }

    // Finds the Start room among the builder's spawned rooms and moves the player there, with CharacterController temporarily disabled
    private void TeleportPlayerToStart()
    {
        var rooms = builder.GetSpawnedRooms();

        if (rooms == null || rooms.Count == 0) return;

        NodeInstance startRoom = rooms.Find(r => r.kind == RoomKind.Start);

        Vector3 targetPos = (startRoom != null) ? startRoom.transform.position : rooms[0].transform.position;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.position = targetPos + Vector3.up * 1.2f;

        if (cc != null) cc.enabled = true;
    }

    // Updates the visibility manager's map container and room list to match the current floor
    private void SyncSystemsWithCurrentFloor()
    {
        visibilityManager.currentMapContainer = mapFolders[currentFloor - 1].transform;

        if (roomsPerFloor.ContainsKey(currentFloor))
        {
            visibilityManager.RegisterRooms(roomsPerFloor[currentFloor]);
        }
    }

    // Destroys all floor and map folders, resets state, and returns the player to the world position
    private void ResetAndExitDungeon()
    {
        foreach (var f in floorFolders) Destroy(f);
        foreach (var m in mapFolders) Destroy(m);

        floorFolders.Clear();
        mapFolders.Clear();
        builder.ClearLists();

        currentFloor = 0;
        isInsideDungeon = false;
        player.position = worldReturnPosition;
    }

    // Goes back one floor, or exits to the world if already on floor 1
    public void GoToPreviousFloor()
    {
        if (currentFloor <= 1)
        {
            ExitToWorld();
            return;
        }

        uiManager.OpenPanel(UIManager.PanelType.Loading);

        floorFolders[currentFloor - 1].SetActive(false);
        mapFolders[currentFloor - 1].SetActive(false);

        currentFloor--;

        floorFolders[currentFloor - 1].SetActive(true);
        mapFolders[currentFloor - 1].SetActive(true);

        SyncSystemsWithCurrentFloor();
        TeleportPlayerToStart();

        uiManager.CloseCurrentPanel();
        Debug.Log($"Returned to floor {currentFloor}");
    }

    // Starts the coroutine that generates and activates the next floor
    public void StartNextFloor()
    {
        StartCoroutine(GenerateFloorRoutine());
    }

    // Coroutine: shows the loading screen, generates a new floor folder if needed, syncs systems, and teleports the player to the start room
    private IEnumerator GenerateFloorRoutine()
    {
        uiManager.OpenPanel(UIManager.PanelType.Loading);

        yield return null;

        if (currentFloor >= maxFloors)
        {
            ResetAndExitDungeon();
            if (worldRoot != null) worldRoot.SetActive(true);
            uiManager.CloseCurrentPanel();
            yield break;
        }

        if (currentFloor > 0)
        {
            floorFolders[currentFloor - 1].SetActive(false);
            mapFolders[currentFloor - 1].SetActive(false);
        }

        currentFloor++;
        isInsideDungeon = true;

        if (currentFloor > floorFolders.Count)
        {
            builder.config.seed += 1;

            GameObject floorRoot = new GameObject($"Floor_{currentFloor}");
            floorRoot.transform.SetParent(dungeonSystemRoot);
            floorFolders.Add(floorRoot);

            GameObject mapRoot = new GameObject($"Map_Floor_{currentFloor}");
            mapRoot.transform.SetParent(mapContainerRoot);
            mapFolders.Add(mapRoot);

            builder.BuildInFolder(floorRoot.transform, mapRoot.transform);

            roomsPerFloor[currentFloor] = new List<NodeInstance>(builder.GetSpawnedRooms());
        }
        else
        {
            floorFolders[currentFloor - 1].SetActive(true);
            mapFolders[currentFloor - 1].SetActive(true);
        }

        SyncSystemsWithCurrentFloor();
        TeleportPlayerToStart();

        uiManager.CloseCurrentPanel();
        Debug.Log($"Floor {currentFloor} generated with {builder.GetSpawnedRooms().Count} rooms.");
    }

    // Resets the player's Rigidbody velocity and teleports them back to the world return position
    public void RespawnPlayerInWorld()
    {
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            player.position = worldReturnPosition;
        }
    }

    // Starts the coroutine that transitions the player from the world into the dungeon
    public void EnterDungeon()
    {
        StartCoroutine(EnterDungeonRoutine());
    }

    // Coroutine: shows the loading screen, hides the world root, then generates floor 1 or re-enters the existing current floor
    private IEnumerator EnterDungeonRoutine()
    {
        uiManager.OpenPanel(UIManager.PanelType.Loading);

        yield return null;

        if (worldRoot != null)
        {
            worldRoot.SetActive(false);
        }

        if (currentFloor == 0)
        {
            yield return StartCoroutine(GenerateFloorRoutine());
        }
        else
        {
            isInsideDungeon = true;
            TeleportPlayerToStart();
            floorFolders[currentFloor - 1].SetActive(true);
            mapFolders[currentFloor - 1].SetActive(true);
            uiManager.CloseCurrentPanel();
        }
    }

    // Sets the total floor count and enters the dungeon to begin a new run
    public void StartNewDungeon(int floors)
    {
        maxFloors = floors;
        EnterDungeon();
    }

    // Re-enters the dungeon at the current floor without resetting state
    public void ResumeDungeon()
    {
        EnterDungeon();
    }

    // Exits to the world without destroying dungeon data
    public void ExitDungeonToWorld()
    {
        ExitToWorld();
    }

    // Exits to the world and destroys all dungeon floors and map data
    public void ExitAndDeleteDungeon()
    {
        ResetAndExitDungeon();
        if (worldRoot != null) worldRoot.SetActive(true);
    }

    // Destroys all dungeon floor and map folders and resets the floor counter while keeping the player in the world
    public void DeleteDungeonFromWorld()
    {
        foreach (var f in floorFolders) Destroy(f);
        foreach (var m in mapFolders) Destroy(m);

        floorFolders.Clear();
        mapFolders.Clear();
        builder.ClearLists();
        currentFloor = 0;
    }
}
