//
// DungeonVisibilityManager controls which dungeon rooms are visible at any given time.
// On a configurable interval it determines which room the player is standing in,
// spawns enemies on first entry, and activates only the current room, the previous
// room, and all immediate neighbors. It also marks rooms as discovered on the minimap.
//

using System.Collections.Generic;
using UnityEngine;

public class DungeonVisibilityManager : MonoBehaviour
{
    [InjectAttribute1] public IThirdPersonCharacter Player { get; set; }
    [InjectAttribute1] public IEnemySpawner EnemySpawner { get; set; }
    [SerializeField] private float checkInterval = 0.2f;
    [SerializeField] private float boundsPadding = 1.5f;
    [HideInInspector] public Transform currentMapContainer;

    private List<NodeInstance> allRooms = new();
    private readonly Dictionary<NodeInstance, Bounds> roomBoundsCache = new();

    private NodeInstance currentRoom;
    private NodeInstance previousRoom;

    private float timer;

    // Injects dependencies via the dependency container
    private void Start()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    // Replaces the room list with the given set, clears previous state, and pre-computes the renderer-based bounds for each room
    public void RegisterRooms(List<NodeInstance> rooms)
    {
        allRooms.Clear();
        allRooms.AddRange(rooms);

        currentRoom = null;
        previousRoom = null;

        roomBoundsCache.Clear();
        foreach (var room in allRooms)
        {
            if (room != null)
            {
                roomBoundsCache[room] = CalculateRoomBounds(room);
            }
        }
    }

    // Computes the combined Renderer bounds for all children of a room, including inactive ones; returns a minimal bounds if none are found
    private Bounds CalculateRoomBounds(NodeInstance room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return new Bounds(room.transform.position, Vector3.one * 2f);
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        return b;
    }

    // Resets the check timer and immediately refreshes room visibility after a player teleport
    public void NotifyPlayerTeleported()
    {
        timer = 0f;
        RefreshVisibility();
    }

    // Updates the map container reference used when discovering rooms on the minimap
    public void SetCurrentMapContainer(Transform newMapFolder)
    {
        currentMapContainer = newMapFolder;
    }

    // On a regular interval, refreshes room visibility and marks the current room as discovered on the minimap
    private void Update()
    {
        if (Player == null || allRooms.Count == 0) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        RefreshVisibility();

        if (currentRoom != null)
        {
            currentRoom.Discover(currentMapContainer);
        }
    }

    // Detects the player's current room, spawns enemies on first entry, and applies the visibility set if the room has changed
    private void RefreshVisibility()
    {
        Transform pTransform = ((MonoBehaviour)Player).transform;
        NodeInstance detected = DetectCurrentRoom(pTransform.position);

        if (detected == null || detected == currentRoom) return;

        if (detected.kind != RoomKind.Start && !detected.hasSpawnedEnemy)
        {
            if (detected.spawnPoint != null && EnemySpawner != null)
            {
                EnemySpawner.SpawnEnemyAt(detected.spawnPoint, detected.transform);
                detected.hasSpawnedEnemy = true;
                Debug.Log($"[Spawner] Enemy spawned in new room: {detected.name}");
            }
        }

        previousRoom = currentRoom;
        currentRoom = detected;
        ApplyVisibility(currentRoom);
    }

    // Checks each room's padded bounds to find the one containing the player; falls back to the closest room by center distance
    private NodeInstance DetectCurrentRoom(Vector3 pos)
    {
        foreach (var room in allRooms)
        {
            if (room == null) continue;
            if (IsInsideRoom(room, pos)) return room;
        }
        return FindClosestRoom(pos);
    }

    // Computes combined Renderer bounds for a room using only active renderers (legacy version used alongside CalculateRoomBounds)
    private Bounds CalcRoomBounds(NodeInstance room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(room.transform.position, Vector3.one * 2f);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return b;
    }

    // Returns true if the position falls within the cached bounds of a room expanded by the padding margin
    private bool IsInsideRoom(NodeInstance room, Vector3 pos)
    {
        if (!roomBoundsCache.TryGetValue(room, out Bounds b)) return false;

        Bounds padded = b;
        padded.Expand(boundsPadding);
        return padded.Contains(pos);
    }

    // Returns the room whose cached bounds center is closest to the given position
    private NodeInstance FindClosestRoom(Vector3 pos)
    {
        NodeInstance closest = null;
        float minDist = float.MaxValue;

        foreach (var room in allRooms)
        {
            if (room == null) continue;
            Vector3 center = roomBoundsCache.TryGetValue(room, out Bounds b)
                ? b.center
                : room.transform.position;

            float d = Vector3.SqrMagnitude(center - pos);
            if (d < minDist)
            {
                minDist = d;
                closest = room;
            }
        }
        return closest;
    }

    // Activates the current room, the previous room, and all neighbors; deactivates every other room
    private void ApplyVisibility(NodeInstance room)
    {
        HashSet<NodeInstance> visible = new();

        visible.Add(room);

        if (previousRoom != null)
            visible.Add(previousRoom);

        foreach (var neighbour in room.neighbors)
        {
            if (neighbour != null)
                visible.Add(neighbour);
        }

        foreach (var r in allRooms)
        {
            if (r == null) continue;
            r.gameObject.SetActive(visible.Contains(r));
        }
    }

    // Sets every room in the list to the given active state
    private void SetAllVisible(bool visible)
    {
        foreach (var room in allRooms)
            if (room != null) SetRoomVisible(room, visible);
    }

    // Sets a single room's GameObject active state, disabling its renderers, colliders, and scripts when hidden
    private void SetRoomVisible(NodeInstance room, bool visible)
    {
        room.gameObject.SetActive(visible);
    }
}
