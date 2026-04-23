using UnityEngine;

// teleport-only
public class ChainGenerator : MonoBehaviour
{
    [SerializeField] private Transform player;

    private void Start()
    {
        Debug.Log("[GEN] ChainGenerator in teleport-only mode");
    }

    public void Interact(DoorPortal usedPortal, Collider pressedCollider)
    {
        if (usedPortal == null || player == null) return;

        if (usedPortal.linked == null)
        {
            Debug.LogWarning($"[GEN] {usedPortal.name} has no linked");
            return;
        }

        Transform t = null;
        if (pressedCollider == usedPortal.colliderA) t = usedPortal.spawnA;
        else if (pressedCollider == usedPortal.colliderB) t = usedPortal.spawnB;
        else return;

        if (t == null) return;

        player.position = t.position;
        player.rotation = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
    }
}