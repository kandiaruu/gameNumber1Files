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

    private readonly HashSet<DoorPortal> used = new();

    private void Awake()
    {
        if (portals == null) return;
        foreach (var p in portals)
            if (p != null) p.owner = this;
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