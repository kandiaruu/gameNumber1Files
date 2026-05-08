//
// NodeInstance is the MonoBehaviour representation of a dungeon room in the scene.
// It holds the room's logical kind, its door portals, neighbor references, a spawn
// point for enemies, and map/chest settings. It handles chest spawning on Start,
// minimap discovery, and provides portal management helpers for the dungeon generator.
//

using System.Collections.Generic;
using UnityEngine;

public class NodeInstance : MonoBehaviour
{
    [Header("Logical kind")]
    public RoomKind kind;

    [Header("Portals in this room/piece")]
    public DoorPortal[] portals;

    [Header("Attach marker (where prefab must be snapped)")]
    public Transform attachFromSide;
    [Header("Spawn Settings")]
    public Transform spawnPoint;

    public List<NodeInstance> neighbors = new List<NodeInstance>();
    [Header("Map Settings")]
    public GameObject mapVisual;
    [Header("Chest Spawning")]
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private Transform[] chestSpawnSpots;
    [SerializeField, Range(0f, 100f)] private float chestSpawnChance = 10f;

    private bool isDiscovered = false;
    [HideInInspector] public bool hasSpawnedEnemy = false;

    private readonly HashSet<DoorPortal> used = new();

    // Assigns this NodeInstance as the owner of each of its door portals
    private void Awake()
    {
        if (portals == null) return;
        foreach (var p in portals)
            if (p != null) p.owner = this;
    }

    // Triggers chest spawning when the room is first created
    private void Start()
    {
        SpawnChests();
    }

    // Iterates over all designated spawn spots and instantiates a chest prefab at each one based on a random chance roll
    private void SpawnChests()
    {
        if (chestPrefab == null || chestSpawnSpots == null || chestSpawnSpots.Length == 0)
            return;

        foreach (Transform spot in chestSpawnSpots)
        {
            float randomValue = Random.Range(0f, 100f);
            if (randomValue <= chestSpawnChance)
            {
                Instantiate(chestPrefab, spot.position, spot.rotation, transform);
            }
        }
    }

    // Adds another NodeInstance to this room's neighbor list, avoiding duplicates and self-references
    public void AddNeighbor(NodeInstance other)
    {
        if (other != null && other != this && !neighbors.Contains(other))
        {
            neighbors.Add(other);
        }
    }

    // Marks the room as discovered, activates and re-parents its map icon into the given map container so it persists when the room is hidden
    public void Discover(Transform mapContainer)
    {
        if (isDiscovered || mapVisual == null) return;

        isDiscovered = true;
        mapVisual.SetActive(true);
        mapVisual.name = this.gameObject.name;

        if (mapContainer != null)
            mapVisual.transform.SetParent(mapContainer);
        else
            mapVisual.transform.SetParent(null);
    }

    // Returns the first portal that has not yet been marked as used, or null if all are used
    public DoorPortal GetFreePortal()
    {
        if (portals == null) return null;
        foreach (var p in portals)
            if (p != null && !used.Contains(p))
                return p;
        return null;
    }

    // Returns the count of portals that have not yet been marked as used
    public int GetFreeCount()
    {
        if (portals == null) return 0;
        int c = 0;
        foreach (var p in portals)
            if (p != null && !used.Contains(p)) c++;
        return c;
    }

    // Marks a portal as used so it is no longer returned by GetFreePortal or GetAllFree
    public void MarkUsed(DoorPortal p)
    {
        if (p != null) used.Add(p);
    }

    // Enumerates all portals that have not yet been marked as used
    public IEnumerable<DoorPortal> GetAllFree()
    {
        if (portals == null) yield break;
        foreach (var p in portals)
            if (p != null && !used.Contains(p)) yield return p;
    }
}
