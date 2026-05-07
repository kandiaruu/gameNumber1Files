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
    // Список всех физически соединенных комнат
    // [HideInInspector] 
    public List<NodeInstance> neighbors = new List<NodeInstance>();
    [Header("Map Settings")]
    public GameObject mapVisual; // Сюда в инспекторе перетащи объект MapVisual
    [Header("Chest Spawning")]
    [SerializeField] private GameObject chestPrefab; // Сюда закинем префаб сундука
    [SerializeField] private Transform[] chestSpawnSpots; // Точки, где могут появиться сундуки
    [SerializeField, Range(0f, 100f)] private float chestSpawnChance = 10f; // Шанс 10% для каждой точки

    private bool isDiscovered = false;
    [HideInInspector] public bool hasSpawnedEnemy = false;

    private readonly HashSet<DoorPortal> used = new();

    private void Awake()
    {
        if (portals == null) return;
        foreach (var p in portals)
            if (p != null) p.owner = this;
    }

    private void Start()
    {
        // Вызываем спавн сундуков при появлении комнаты
        SpawnChests();
    }

    private void SpawnChests()
    {
        // Проверяем, назначены ли префаб и точки спавна
        if (chestPrefab == null || chestSpawnSpots == null || chestSpawnSpots.Length == 0) 
            return;

        foreach (Transform spot in chestSpawnSpots)
        {
            // Случайное число от 0 до 100. Если оно меньше или равно нашему шансу (10) — спавним сундук
            float randomValue = Random.Range(0f, 100f);
            if (randomValue <= chestSpawnChance)
            {
                // Создаем сундук, делая его дочерним объектом комнаты (transform)
                Instantiate(chestPrefab, spot.position, spot.rotation, transform);
            }
        }
    }

    // Метод для безопасного добавления соседа
    public void AddNeighbor(NodeInstance other)
    {
        if (other != null && other != this && !neighbors.Contains(other))
        {
            neighbors.Add(other);
        }
    }

    public void Discover(Transform mapContainer)
    {
        if (isDiscovered || mapVisual == null) return;

        isDiscovered = true;
        mapVisual.SetActive(true); 
        mapVisual.name = this.gameObject.name;
        
        // Хитрый ход: чтобы иконка не исчезла, когда менеджер видимости выключит комнату,
        // мы можем отцепить иконку от родителя.
        if (mapContainer != null)
            mapVisual.transform.SetParent(mapContainer);
        else
            mapVisual.transform.SetParent(null);
    }

    public DoorPortal GetFreePortal()
    {
        if (portals == null) return null;
        foreach (var p in portals)
            if (p != null && !used.Contains(p))
                return p;
        return null;
    }

    public int GetFreeCount()
    {
        if (portals == null) return 0;
        int c = 0;
        foreach (var p in portals)
            if (p != null && !used.Contains(p)) c++;
        return c;
    }

    public void MarkUsed(DoorPortal p)
    {
        if (p != null) used.Add(p);
    }

    public IEnumerable<DoorPortal> GetAllFree()
    {
        if (portals == null) yield break;
        foreach (var p in portals)
            if (p != null && !used.Contains(p)) yield return p;
    }
}