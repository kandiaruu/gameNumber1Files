using System.Collections.Generic;
using UnityEngine;

public class DungeonVisibilityManager : MonoBehaviour
{
    [InjectAttribute1] public IThirdPersonCharacter Player { get; set; }
    [InjectAttribute1] public IEnemySpawner EnemySpawner { get; set; }
    [SerializeField] private float checkInterval = 0.2f; // как часто проверять комнату игрока (сек)
    [SerializeField] private float boundsPadding = 1.5f; // запас при проверке — игрок должен выйти дальше этого от краёв комнаты чтобы она перестала считаться текущей
    [HideInInspector] public Transform currentMapContainer;

    // Все комнаты данжа
    private List<NodeInstance> allRooms = new();

    // Закэшированные bounds для каждой комнаты (считаются один раз после RegisterRooms)
    private readonly Dictionary<NodeInstance, Bounds> roomBoundsCache = new();

    // Текущая комната игрока
    // DungeonVisibilityManager.cs

    private NodeInstance currentRoom;
    private NodeInstance previousRoom; // Добавьте это поле для хранения "истории"

    private float timer;

    // ─────────────────────────────────────────
    // Публичное API
    // ─────────────────────────────────────────
    private void Start()
    {
        // Инициализируем зависимости через ваш контейнер[cite: 9]
        DependencyContainer1.InjectDependencies(this);
    }

    /// Вызови из DungeonBuilder после окончания генерации.
public void RegisterRooms(List<NodeInstance> rooms)
{
    allRooms.Clear();
    allRooms.AddRange(rooms);

    // СБРОС: Менеджер должен забыть про комнаты старого этажа
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

private Bounds CalculateRoomBounds(NodeInstance room)
{
    // ДОБАВЛЕН ПАРАМЕТР true
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
    /// Вызови из ChainGenerator.Interact() сразу после телепорта игрока,
    /// чтобы видимость обновилась мгновенно без задержки.
    public void NotifyPlayerTeleported()
    {
        timer = 0f;
        RefreshVisibility();
    }

    // ─────────────────────────────────────────
    // Unity
    // ─────────────────────────────────────────

// DungeonVisibilityManager.cs[cite: 18]

// Метод для обновления папки карты при смене этажа
    public void SetCurrentMapContainer(Transform newMapFolder)
    {
        currentMapContainer = newMapFolder;
    }

    private void Update()
    {
        // Проверяем наличие игрока через свойство, установленное DI
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

    // ─────────────────────────────────────────
    // Основная логика
    // ─────────────────────────────────────────

    private void RefreshVisibility()
    {
        // Получаем позицию через трансформ игрока (нужно убедиться, что IThirdPersonCharacter дает доступ к нему)
        Transform pTransform = ((MonoBehaviour)Player).transform;
        NodeInstance detected = DetectCurrentRoom(pTransform.position);
        
        if (detected == null || detected == currentRoom) return;

        // ЛОГИКА СПАВНА ВРАГА
        // Проверяем: не начальная комната, еще не спавнили, и есть точка спавна
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

    private NodeInstance DetectCurrentRoom(Vector3 pos)
    {
        foreach (var room in allRooms)
        {
            if (room == null) continue;
            if (IsInsideRoom(room, pos)) return room;
        }
        return FindClosestRoom(pos);
    }

    /// Считает объединённый Bounds всех рендереров комнаты.
    /// Вызывается один раз при RegisterRooms и кэшируется.
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

    private bool IsInsideRoom(NodeInstance room, Vector3 pos)
    {
        if (!roomBoundsCache.TryGetValue(room, out Bounds b)) return false;

        // Расширяем bounds на padding — игрок должен уйти дальше этого расстояния
        // от краёв чтобы комната перестала считаться текущей
        Bounds padded = b;
        padded.Expand(boundsPadding);
        return padded.Contains(pos);
    }

    /// Фолбэк: ближайшая комната по центру bounds (не по pivot'у GameObject'а).
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

    /// Показывает текущую комнату + всех соседей, прячет всё остальное.
    private void ApplyVisibility(NodeInstance room)
    {
        HashSet<NodeInstance> visible = new();
        
        // 1. Текущая комната
        visible.Add(room);

        // 2. Предыдущая (для плавности перехода, как у тебя и было)
        if (previousRoom != null)
            visible.Add(previousRoom);

        // 3. ВСЕ соседи (теперь это работает железно)
        foreach (var neighbour in room.neighbors)
        {
            if (neighbour != null)
                visible.Add(neighbour);
        }

        // Применяем результат
        foreach (var r in allRooms)
        {
            if (r == null) continue;
            r.gameObject.SetActive(visible.Contains(r));
        }
    }

    // ─────────────────────────────────────────
    // Утилиты
    // ─────────────────────────────────────────

    private void SetAllVisible(bool visible)
    {
        foreach (var room in allRooms)
            if (room != null) SetRoomVisible(room, visible);
    }

    private void SetRoomVisible(NodeInstance room, bool visible)
    {
        // Включаем/выключаем весь GameObject комнаты.
        // Это отключает рендеры, коллайдеры и скрипты — максимальная экономия.
        room.gameObject.SetActive(visible);
    }
}