using UnityEngine;

public class EnemySpawner : MonoBehaviour, IEnemySpawner
{
    public GameObject enemyPrefab;
    [InjectAttribute1] public IPlayerStats playerStats { get; set; }
    [InjectAttribute1] public IDungeonFloorManager floorManager { get; set; } // <--- Добавлено

    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    public void SpawnEnemyAt(Transform point, Transform parent)
    {
        if (enemyPrefab == null || point == null) return;

        int amountToSpawn = 1;

        // Приводим интерфейс к классу, чтобы получить доступ к maxFloors
        var manager = floorManager as DungeonFloorManager;
        if (manager != null)
        {
            // От А ранга (32 этажа и выше)
            if (manager.maxFloors >= 32) amountToSpawn = 3;
            // От D ранга (4 этажа и выше)
            else if (manager.maxFloors >= 4) amountToSpawn = 2;
        }

        // Проверка на комнату босса. Если в комнате есть BossRoomPortal или это тип Portal
        if (parent != null)
        {
            NodeInstance room = parent.GetComponent<NodeInstance>();
            if (room != null && room.kind == RoomKind.Portal)
            {
                amountToSpawn = 1; // В босс комнате всегда 1
            }
            else if (parent.GetComponentInChildren<BossRoomPortal>() != null)
            {
                amountToSpawn = 1;
            }
        }

        // Спавним нужное количество монстров
        for (int i = 0; i < amountToSpawn; i++)
        {
            // Добавляем небольшое случайное смещение, чтобы враги не застревали друг в друге
            Vector3 offset = Vector3.zero;
            if (amountToSpawn > 1)
            {
                offset = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
            }

            Instantiate(enemyPrefab, point.position + offset, point.rotation, parent);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) SpawnEnemyAt(player.transform, null);
        }
    }
}