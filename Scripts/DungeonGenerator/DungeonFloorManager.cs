using System.Collections.Generic;
using System.Collections; // Нужно для IEnumerator
using UnityEngine;

public class DungeonFloorManager : MonoBehaviour, IDungeonFloorManager
{
    [Header("Dungeon Settings")]
    public int maxFloors = 6; // <--- Переменная для хранения длины подземелья
    [Header("Roots")]
    public Transform dungeonSystemRoot; // Папка для этажей
    public Transform mapContainerRoot;  // Папка для иконок карты
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
    public bool isInsideDungeon = false; // Флаг: игрок в данже или в мире
    
    private List<GameObject> floorFolders = new List<GameObject>();
    private List<GameObject> mapFolders = new List<GameObject>();
    private Vector3 worldReturnPosition; // Координаты портала в мире

    private void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        // Запоминаем, где стоит игрок в начале (у портала в мире)
        worldReturnPosition = player.position; 
    }

    public bool IsInsideDungeon => isInsideDungeon;
    public bool HasActiveDungeon => currentFloor > 0 || floorFolders.Count > 0;

    // ЭТОТ МЕТОД ВЫЗЫВАЕТСЯ ПРИ НАЖАТИИ НА ПОРТАЛ (DungeonEntryPortal)
    public void OnPortalInteract()
    {
        if (isInsideDungeon)
        {
            // Случай А: Игрок нажал портал ВНУТРИ данжа -> Выходим в мир
            ExitToWorld();
        }
        else
        {
            // Случай Б: Игрок нажал портал в МИРЕ -> Входим в данж
            EnterDungeon();
        }
    }

    private void ExitToWorld()
    {
        uiManager.OpenPanel(UIManager.PanelType.Loading);

        isInsideDungeon = false;
        
        // Перемещаем игрока к порталу в мире
        player.position = worldReturnPosition;

        // НОВОЕ: Включаем стартовую локацию
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

private void TeleportPlayerToStart()
{
    // Получаем актуальный список комнат через метод вашего билдера
    var rooms = builder.GetSpawnedRooms();

    // Защита от ArgumentOutOfRangeException
    if (rooms == null || rooms.Count == 0) return;

    // Ищем стартовую комнату в полученном списке
    NodeInstance startRoom = rooms.Find(r => r.kind == RoomKind.Start);
    
    // Если StartRoom найдена - берем её позицию, иначе - позицию самой первой сгенерированной комнаты
    Vector3 targetPos = (startRoom != null) ? startRoom.transform.position : rooms[0].transform.position;

    // Важно для CharacterController (отключаем на момент телепортации)
    var cc = player.GetComponent<CharacterController>();
    if (cc != null) cc.enabled = false;
    
    player.position = targetPos + Vector3.up * 1.2f;
    
    if (cc != null) cc.enabled = true;
}

private void SyncSystemsWithCurrentFloor()
{
    // Устанавливаем контейнер для карты
    visibilityManager.currentMapContainer = mapFolders[currentFloor - 1].transform;
    
    // Берем список комнат из нашего словаря по номеру этажа
    if (roomsPerFloor.ContainsKey(currentFloor))
    {
        visibilityManager.RegisterRooms(roomsPerFloor[currentFloor]);
    }
}

    private void ResetAndExitDungeon()
    {
        // Полная очистка при завершении 6 уровней[cite: 2, 4]
        foreach (var f in floorFolders) Destroy(f);
        foreach (var m in mapFolders) Destroy(m);
        
        floorFolders.Clear();
        mapFolders.Clear();
        builder.ClearLists();
        
        currentFloor = 0;
        isInsideDungeon = false;
        player.position = worldReturnPosition;
    }

    public void GoToPreviousFloor()
    {
        if (currentFloor <= 1)
        {
            // Если мы уже на 1-м этаже, то выходим в мир
            ExitToWorld();
            return;
        }

        uiManager.OpenPanel(UIManager.PanelType.Loading);

        // Выключаем текущий этаж
        floorFolders[currentFloor - 1].SetActive(false);
        mapFolders[currentFloor - 1].SetActive(false);

        // Уменьшаем счетчик этажа
        currentFloor--;

        // Включаем предыдущий этаж
        floorFolders[currentFloor - 1].SetActive(true);
        mapFolders[currentFloor - 1].SetActive(true);

        // Синхронизируем системы для старого этажа
        SyncSystemsWithCurrentFloor();
        TeleportPlayerToStart();

        uiManager.CloseCurrentPanel();
        Debug.Log($"Спустились обратно на этаж {currentFloor}");
    }

    public void StartNextFloor()
    {
        StartCoroutine(GenerateFloorRoutine());
    }

    // Основная логика теперь здесь
    private IEnumerator GenerateFloorRoutine()
    {
        // 1. Включаем экран загрузки
        uiManager.OpenPanel(UIManager.PanelType.Loading);

        // 2. Ждем до конца кадра, чтобы Unity успела отрисовать UI
        yield return null;
        
        // Проверка лимита этажей
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

            // ТЯЖЕЛАЯ ГЕНЕРАЦИЯ (теперь UI уже на экране)[cite: 2]
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

        // 3. Выключаем экран загрузки
        uiManager.CloseCurrentPanel();
        Debug.Log($"Floor {currentFloor} generated with {builder.GetSpawnedRooms().Count} rooms.");
    }

    public void RespawnPlayerInWorld()
    {
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Сбрасываем инерцию падения перед телепортом
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            player.position = worldReturnPosition; 
        }
    }

    // То же самое для входа в данж
    public void EnterDungeon()
    {
        StartCoroutine(EnterDungeonRoutine());
    }

    private IEnumerator EnterDungeonRoutine()
    {
        uiManager.OpenPanel(UIManager.PanelType.Loading);
        
        // Ждем кадр, чтобы UI точно появился
        yield return null;

        // НОВОЕ: Выключаем стартовую локацию, пока висит экран загрузки
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

    public void StartNewDungeon(int floors)
    {
        maxFloors = floors;
        EnterDungeon();
    }

    public void ResumeDungeon()
    {
        EnterDungeon();
    }

    public void ExitDungeonToWorld()
    {
        ExitToWorld();
    }

    public void ExitAndDeleteDungeon()
    {
        ResetAndExitDungeon();
        if (worldRoot != null) worldRoot.SetActive(true); // Включаем мир
    }

    public void DeleteDungeonFromWorld()
    {
        // Удаляем подземелье, оставаясь в мире
        foreach (var f in floorFolders) Destroy(f);
        foreach (var m in mapFolders) Destroy(m);
        
        floorFolders.Clear();
        mapFolders.Clear();
        builder.ClearLists();
        currentFloor = 0;
    }
}