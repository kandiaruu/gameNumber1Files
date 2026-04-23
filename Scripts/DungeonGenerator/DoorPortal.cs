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
    [HideInInspector] public DoorPortal linked;
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

    public Vector3 SideDir
    {
        get
        {
            Vector3 d = side ? side.forward : transform.forward;
            d.y = 0f;
            return d.normalized;
        }
    }

    public Vector3 SocketPos => socket ? socket.position : transform.position;
}