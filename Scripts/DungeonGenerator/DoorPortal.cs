//
// DoorPortal defines a two-sided door connection between dungeon rooms.
// It stores the two colliders the player can press, the matching teleport
// spawn points, directional data used by the dungeon generator for snapping,
// and a socket point for placement alignment.
//

using UnityEngine;

public enum DoorSideType
{
    Forward,
    Left,
    Right
}

public class DoorPortal : MonoBehaviour
{
    [HideInInspector] public NodeInstance owner;
    [Header("Direction")]
    public DoorSideType sideType;

    [Header("2 colliders on ONE door")]
    public Collider colliderA;
    public Collider colliderB;

    [Header("Teleport points (A->A, B->B)")]
    public Transform spawnA;
    public Transform spawnB;

    [Header("Door forward for generation")]
    public Transform side;

    [Header("Door socket point")]
    public Transform socket;

    // Returns the horizontal forward direction of this door, used by the dungeon generator to align new rooms
    public Vector3 SideDir
    {
        get
        {
            Vector3 d = side ? side.forward : transform.forward;
            d.y = 0f;
            return d.normalized;
        }
    }

    // Returns the world position of the socket point, used as the snap anchor during room placement
    public Vector3 SocketPos => socket ? socket.position : transform.position;
}
