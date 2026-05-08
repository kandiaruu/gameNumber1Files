//
// EnemySpawner handles spawning enemies at designated points in the dungeon.
// It scales the number of enemies per spawn point based on the dungeon's floor count
// (difficulty rank), always caps boss-room spawns to one enemy, and applies a small
// random positional offset when spawning multiple enemies to prevent overlapping.
// A debug hotkey (3) allows spawning an enemy on the player's position at runtime.
//

using UnityEngine;

public class EnemySpawner : MonoBehaviour, IEnemySpawner
{
    public GameObject enemyPrefab;
    [InjectAttribute1] public IPlayerStats playerStats { get; set; }
    [InjectAttribute1] public IDungeonFloorManager floorManager { get; set; }

    // Injects dependencies immediately on creation so they are available before Start
    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    // Spawns one or more enemies at the given point, scaling the count by dungeon rank and capping boss rooms at one
    public void SpawnEnemyAt(Transform point, Transform parent)
    {
        if (enemyPrefab == null || point == null) return;

        int amountToSpawn = 1;

        var manager = floorManager as DungeonFloorManager;
        if (manager != null)
        {
            if (manager.maxFloors >= 32) amountToSpawn = 3;
            else if (manager.maxFloors >= 4) amountToSpawn = 2;
        }

        if (parent != null)
        {
            NodeInstance room = parent.GetComponent<NodeInstance>();
            if (room != null && room.kind == RoomKind.Portal)
            {
                amountToSpawn = 1;
            }
            else if (parent.GetComponentInChildren<BossRoomPortal>() != null)
            {
                amountToSpawn = 1;
            }
        }

        for (int i = 0; i < amountToSpawn; i++)
        {
            Vector3 offset = Vector3.zero;
            if (amountToSpawn > 1)
            {
                offset = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
            }

            Instantiate(enemyPrefab, point.position + offset, point.rotation, parent);
        }
    }
}
