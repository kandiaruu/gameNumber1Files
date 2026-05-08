//
// ChainGenerator handles player teleportation through door portals in the dungeon.
// When the player interacts with a door collider it determines which spawn point
// to use, moves the player there, and notifies the DungeonVisibilityManager.
//

using UnityEngine;

public class ChainGenerator : MonoBehaviour
{
    [SerializeField] private Transform player;

    // Logs that the generator is running in teleport-only mode
    private void Start()
    {
        Debug.Log("[GEN] ChainGenerator in teleport-only mode");
    }

    // Teleports the player to the spawn point that corresponds to the collider they pressed on the given portal, then triggers a visibility refresh
    public void Interact(DoorPortal usedPortal, Collider pressedCollider)
    {
        if (usedPortal == null || player == null) return;

        Transform t = null;
        if (pressedCollider == usedPortal.colliderA) t = usedPortal.spawnA;
        else if (pressedCollider == usedPortal.colliderB) t = usedPortal.spawnB;
        else return;

        if (t == null) return;

        player.position = t.position;
        player.rotation = Quaternion.Euler(0f, t.eulerAngles.y, 0f);

        var vis = FindFirstObjectByType<DungeonVisibilityManager>();
        if (vis != null) vis.NotifyPlayerTeleported();
    }
}
