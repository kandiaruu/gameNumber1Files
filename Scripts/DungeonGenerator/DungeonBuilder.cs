using System;
using System.Collections.Generic;
using UnityEngine;

public class DungeonBuilder : MonoBehaviour
{
    [SerializeField] public DungeonConfig config;
    [SerializeField] private RoomPrefabSet prefabs;
    [SerializeField] private Transform root;

    [Header("Snap offsets")]
    [SerializeField] private float roomToCorridorOffset = 0.6f;
    [SerializeField] private float corridorToRoomOffset = 3.0f;
    [Header("Debug")]
    [SerializeField] private bool verbosePlacementDebug = true;
    [SerializeField] private Transform player;
    private Transform currentMapFolder;

    [Header("Portal placement (percent of maxRooms)")]

    private System.Random rng;
    private int nodeIndex;
    private int deadEndIndex;
    private bool portalPlaced;

    private class OpenEnd
    {
        public NodeInstance fromRoom;
        public DoorPortal fromDoor;
        public int branchLength; // длина ветки
    }

    private class PendingDeadEnd
    {
        public DoorPortal ownerDoor;
        public Vector3 position;
        public Quaternion rotation;
        public NodeInstance instance; // живёт в сцене
    }

    private readonly Dictionary<DoorPortal, PendingDeadEnd> pendingDeadEnds = new();

    private readonly Queue<OpenEnd> frontier = new();
    private readonly List<NodeInstance> spawnedRooms = new();

        // 1) Новый ролл
    private RoomKind RollAnyKind()
    {
        double r = rng.NextDouble();
        if (r < 0.33) return RoomKind.Corridor; // 50%
        if (r < 0.50) return RoomKind.Single;   // 30%
        return RoomKind.Triple;                 // 20%
    }

    // В месте, где генеришь новые OpenEnd для triple:

    private bool CanPlaceCorridorThenDeadEnd(OpenEnd end)
    {
        if (end == null || end.fromRoom == null || end.fromDoor == null)
        {
            Dbg("[CHK] end invalid");
            return false;
        }
        var pendingToRestore = PopPendingDeadEnd(end.fromDoor);

        if (prefabs.corridor == null || prefabs.deadEndRoom == null)
        {
            Dbg("[CHK] corridor/deadEnd prefab missing");
            RespawnDeadEnd(pendingToRestore);
            return false;
        }

        Vector3 dir = end.fromDoor.SideDir;
        if (dir.sqrMagnitude < 0.0001f) dir = end.fromRoom.transform.forward;
        dir.y = 0f;
        dir.Normalize();

        Dbg($"[CHK] from={end.fromRoom.name}/{end.fromDoor.name}");
        Dbg($"[CHK] dir={dir}, socket={end.fromDoor.SocketPos}");

        // PROBE 1: corridor
        NodeInstance corrProbe = Instantiate(prefabs.corridor, Vector3.zero, Quaternion.identity, GetRoot());
        corrProbe.name = "PROBE_Corridor";
        DoorPortal corrDoor = GetAnyDoor(corrProbe);
        if (corrDoor == null)
        {
            Dbg("[CHK] FAIL: corridor has no door");
            DestroyImmediate(corrProbe.gameObject);
            RespawnDeadEnd(pendingToRestore);
            return false;
        }

        AlignByAttach(corrProbe, end.fromDoor.SocketPos + dir * roomToCorridorOffset, dir);

        bool corrBlocked = OverlapsPlacedNodes(
            corrProbe, end.fromRoom, null);
        Dbg($"[CHK] corridor pos={corrProbe.transform.position}, blocked={corrBlocked}");
        if (corrBlocked)
        {
            DestroyImmediate(corrProbe.gameObject);
            RespawnDeadEnd(pendingToRestore);
            return false;
        }

        // PROBE 2: deadend after corridor
        NodeInstance deadProbe = Instantiate(prefabs.deadEndRoom, Vector3.zero, Quaternion.identity, GetRoot());
        deadProbe.name = "PROBE_DeadEnd";
        AlignByAttach(deadProbe, corrDoor.SocketPos + dir * corridorToRoomOffset, dir);

        bool deadBlocked = OverlapsPlacedNodes(
            deadProbe, end.fromRoom, corrProbe);
        Dbg($"[CHK] deadend pos={deadProbe.transform.position}, blocked={deadBlocked}");

        DestroyImmediate(deadProbe.gameObject);
        DestroyImmediate(corrProbe.gameObject);

        if (deadBlocked)
        {
            RespawnDeadEnd(pendingToRestore);
            return false;
        }

        Dbg($"[CHK] RESULT corridor+deadend = True");
        return true;
    }

    private bool CanPlaceSingleWithDeadEnd(OpenEnd end)
    {
        if (end == null || end.fromRoom == null || end.fromDoor == null) return false;
        var pendingToRestore = PopPendingDeadEnd(end.fromDoor);

        Vector3 dir = end.fromDoor.SideDir;
        if (dir.sqrMagnitude < 0.0001f) dir = end.fromRoom.transform.forward;
        dir.y = 0f;
        dir.Normalize();

        // 1) single прямо после двери
        NodeInstance single = Instantiate(prefabs.singleRoom, Vector3.zero, Quaternion.identity, GetRoot());
        single.name = "PROBE_Single";
        AlignByAttach(single, end.fromDoor.SocketPos + dir * roomToCorridorOffset, dir);

        bool singleBlocked = OverlapsPlacedNodes(
            single, end.fromRoom, null);

        Dbg($"[CHK-SINGLE] single blocked={singleBlocked}");
        if (singleBlocked)
        {
            DestroyImmediate(single.gameObject);
            RespawnDeadEnd(pendingToRestore);
            return false;
        }

        // 2) тупик после двери single
        DoorPortal outDoor = GetAnyDoor(single);
        if (outDoor == null)
        {
            DestroyImmediate(single.gameObject);
            RespawnDeadEnd(pendingToRestore);
            return false;
        }

        Vector3 outDir = outDoor.SideDir;
        if (outDir.sqrMagnitude < 0.0001f) outDir = single.transform.forward;
        outDir.y = 0f;
        outDir.Normalize();

        NodeInstance dead = Instantiate(prefabs.deadEndRoom, Vector3.zero, Quaternion.identity, GetRoot());
        dead.name = "PROBE_Single_DeadEnd";
        AlignByAttach(dead, outDoor.SocketPos + outDir * roomToCorridorOffset, outDir);

        bool deadBlocked = OverlapsPlacedNodes(
            dead, single, null);

        Dbg($"[CHK-SINGLE] dead blocked={deadBlocked}");

        DestroyImmediate(dead.gameObject);
        DestroyImmediate(single.gameObject);

        if (deadBlocked)
        {
            RespawnDeadEnd(pendingToRestore);
            return false;
        }

        Dbg("[CHK-SINGLE] RESULT=True");
        return true;
    }

    private bool CanPlaceTripleWithDeadEndsAll3(OpenEnd end)
    {
        if (end == null || end.fromRoom == null || end.fromDoor == null) return false;
        var pendingToRestore = PopPendingDeadEnd(end.fromDoor);

        Vector3 dir = end.fromDoor.SideDir;
        if (dir.sqrMagnitude < 0.0001f) dir = end.fromRoom.transform.forward;
        dir.y = 0f;
        dir.Normalize();

        // 1) triple прямо после двери
        NodeInstance triple = Instantiate(prefabs.tripleRoom, Vector3.zero, Quaternion.identity, GetRoot());
        triple.name = "PROBE_Triple";
        AlignByAttach(triple, end.fromDoor.SocketPos + dir * roomToCorridorOffset, dir);

        bool tripleBlocked = OverlapsPlacedNodes(
            triple, end.fromRoom, null);

        Dbg($"[CHK-TRIPLE] triple blocked={tripleBlocked}");
        if (tripleBlocked)
        {
            DestroyImmediate(triple.gameObject);
            RespawnDeadEnd(pendingToRestore);
            return false;
        }

        // 2) список дверей triple
        List<DoorPortal> doors = GetAllDoors(triple);
        if (doors.Count == 0)
        {
            DestroyImmediate(triple.gameObject);
            RespawnDeadEnd(pendingToRestore);
            return false;
        }

        // 3) для каждой двери triple — тупик
        for (int i = 0; i < doors.Count; i++)
        {
            DoorPortal d = doors[i];
            if (d == null)
            {
                DestroyImmediate(triple.gameObject);
                RespawnDeadEnd(pendingToRestore);
                return false;
            }

            Vector3 ddir = d.SideDir;
            if (ddir.sqrMagnitude < 0.0001f) ddir = triple.transform.forward;
            ddir.y = 0f;
            ddir.Normalize();

            NodeInstance dead = Instantiate(prefabs.deadEndRoom, Vector3.zero, Quaternion.identity, GetRoot());
            dead.name = $"PROBE_Triple_Dead_{i}";
            AlignByAttach(dead, d.SocketPos + ddir * roomToCorridorOffset, ddir);

            bool deadBlocked = OverlapsPlacedNodes(
                dead, triple, null);

            Dbg($"[CHK-TRIPLE] branch={i} dead blocked={deadBlocked}");
            DestroyImmediate(dead.gameObject);

            if (deadBlocked)
            {
                DestroyImmediate(triple.gameObject);
                RespawnDeadEnd(pendingToRestore);
                return false;
            }
        }

        DestroyImmediate(triple.gameObject);
        Dbg("[CHK-TRIPLE] RESULT=True");
        return true;
    }

private void RegisterPendingDeadEnd(DoorPortal door, Vector3 dir)
{
    if (door == null) return;

    if (pendingDeadEnds.ContainsKey(door))
        RemoveAndDestroy(door);

    Vector3 pos = door.SocketPos + dir * roomToCorridorOffset;
    NodeInstance inst = Instantiate(prefabs.deadEndRoom, Vector3.zero, Quaternion.identity, GetRoot());
    inst.name = $"PendingDead_{deadEndIndex++}";
    AlignByAttach(inst, pos, dir);
    Physics.SyncTransforms();

    // --- СВЯЗЫВАЕМ ТУПИК С РОДИТЕЛЕМ ---
    NodeInstance parentRoom = door.owner;
    if (parentRoom != null)
    {
        parentRoom.AddNeighbor(inst); // Комната видит тупик
        inst.AddNeighbor(parentRoom); // Тупик видит комнату
    }
    // ----------------------------------

    pendingDeadEnds[door] = new PendingDeadEnd
    {
        ownerDoor = door,
        position = inst.transform.position,
        rotation = inst.transform.rotation,
        instance = inst
    };

    Debug.Log($"[REGISTERERED] {inst.name}, owner={door.owner?.name}");
}

    // Удаляет тупик из сцены и из списка, возвращает данные для респавна
    private PendingDeadEnd PopPendingDeadEnd(DoorPortal door)
    {
        if (door == null || !pendingDeadEnds.TryGetValue(door, out var pending)) return null;
        pendingDeadEnds.Remove(door);
        if (pending.instance != null)
        {
            Debug.Log($"[DELETE] {pending.instance.name}, pos={pending.instance.transform.position}");
            DestroyImmediate(pending.instance.gameObject);
        }
        return pending;
    }

    // Респавнит тупик на то же место (проверка не прошла)
    private void RespawnDeadEnd(PendingDeadEnd pending)
    {
        if (pending == null) return;
        NodeInstance inst = Instantiate(prefabs.deadEndRoom, pending.position, pending.rotation, GetRoot());
        inst.name = $"FinalDead_{deadEndIndex++}";
        spawnedRooms.Add(inst); 
        
        // --- СВЯЗЫВАЕМ ФИНАЛЬНЫЙ ТУПИК ---
        NodeInstance parentRoom = pending.ownerDoor.owner;
        if (parentRoom != null)
        {
            parentRoom.AddNeighbor(inst);
            inst.AddNeighbor(parentRoom);
        }

        Physics.SyncTransforms();
    }

    private void RemoveAndDestroy(DoorPortal door)
    {
        if (pendingDeadEnds.TryGetValue(door, out var p))
        {
            pendingDeadEnds.Remove(door);
            if (p.instance != null) DestroyImmediate(p.instance.gameObject);
        }
    }

    private bool OverlapsPlacedNodes(
        NodeInstance probe,
        NodeInstance ignoreA = null,
        NodeInstance ignoreB = null)
    {
        var probeCols = probe.GetComponentsInChildren<BoxCollider>();
        foreach (var bc in probeCols)
        {
            if (bc == null || !bc.enabled || bc.isTrigger) continue;

            Vector3 worldCenter = bc.transform.TransformPoint(bc.center);
            Vector3 worldSize = Vector3.Scale(bc.size, bc.transform.lossyScale);
            Vector3 half = new Vector3(
                Mathf.Abs(worldSize.x) * 0.475f,
                Mathf.Max(Mathf.Abs(worldSize.y) * 0.475f, 0.05f),
                Mathf.Abs(worldSize.z) * 0.475f
            );

            var hits = Physics.OverlapBox(
                worldCenter,
                half,
                bc.transform.rotation,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.transform.IsChildOf(probe.transform)) continue;
                if (ignoreA != null && h.transform.IsChildOf(ignoreA.transform)) continue;
                if (ignoreB != null && h.transform.IsChildOf(ignoreB.transform)) continue;
                if (h.GetComponentInParent<NodeInstance>() == null) continue;
                if (h.GetComponentInParent<DoorPortal>() != null) continue;
                if (h.GetComponentInParent<DoorInteractable>() != null) continue;

                Dbg($"[BLOCKED] probe={probe.name} заблокирован объектом={h.name}, parent={h.transform.parent?.name}");
                return true;
            }
        }
        return false;
    }

    private void Dbg(string msg)
    {
        if (verbosePlacementDebug) Debug.Log(msg);
    }
    public void BuildInFolder(Transform newRoot, Transform mapFolder)
    {
        this.root = newRoot; // Устанавливаем текущую папку этажа как корень[cite: 14]
        
        // Передаем текущую папку карты в менеджер видимости
        var vis = FindFirstObjectByType<DungeonVisibilityManager>();
        if (vis != null) 
        {
            vis.SetCurrentMapContainer(mapFolder); 
        }

        Build(); // Запускаем генерацию в новую папку[cite: 14]
    }

    // Теперь этот метод чистит только списки, но не удаляет объекты в сцене
    public void ClearLists()
    {
        frontier.Clear();
        spawnedRooms.Clear();
        pendingDeadEnds.Clear();
    }

    // Позволяет FloorManager найти список комнат
    public List<NodeInstance> GetSpawnedRooms()
    {
        return spawnedRooms; // spawnedRooms уже есть в твоем коде[cite: 13]
    }

    public void Build()
    {
        ClearLists();
        if (!ValidateRefs()) return;

        rng = new System.Random(config.seed);
        nodeIndex = 0;
        portalPlaced = false;

        int maxRooms = Mathf.Max(2, config.maxRooms);
        int maxBranchLength = Mathf.Max(2, Mathf.RoundToInt(Mathf.Sqrt(maxRooms)));
        //Mathf.Sqrt(maxRooms))
        float portalFrac = 0.75f;
        int portalTarget = Mathf.Clamp(Mathf.RoundToInt(maxRooms * portalFrac), 2, maxRooms - 1);

        // 1) Start
        NodeInstance start = SpawnRoom(prefabs.startRoom, RoomKind.Start, Vector3.zero, Quaternion.identity);
        if (start == null) return;

        DoorPortal startDoor = GetAnyDoor(start);
        if (startDoor == null)
        {
            Debug.LogError("[BUILD] Start has no door portal");
            return;
        }

        // Главная ветка — специальная очередь
        var mainFrontier = new Queue<OpenEnd>();
        mainFrontier.Enqueue(new OpenEnd { fromRoom = start, fromDoor = startDoor, branchLength = 1 });

        var sideFrontier = new Queue<OpenEnd>();
        int safety = 100000;
        int corSeqCount = 0;

        // 2) Генерация main path до maxBranchLength
        while (mainFrontier.Count > 0 && spawnedRooms.Count < maxRooms && safety-- > 0)
        {
            int maxBranchLength1 = 0;
            foreach (var openEnd in sideFrontier)
            {
                if (openEnd.branchLength > maxBranchLength1)
                    maxBranchLength1 = openEnd.branchLength;
                string roomName1 = openEnd.fromRoom != null ? openEnd.fromRoom.name : "null";
                string doorType1 = openEnd.fromDoor != null ? openEnd.fromDoor.sideType.ToString() : "null";
                string doorName1 = openEnd.fromDoor != null ? openEnd.fromDoor.name : "null";
                Debug.Log($"[MAINFRONTIER] room={roomName1}, door={doorName1}, side={doorType1}, branchLength={openEnd.branchLength}");
            }
            OpenEnd end = mainFrontier.Dequeue();
            Debug.Log($"[DBG] MainPath длина {end.branchLength}, лимит {maxBranchLength} side {maxBranchLength1}");

            if (end.branchLength >= maxBranchLength)
            {
                Debug.Log($"[DBG] MainPath достиг лимита ({end.branchLength}/{maxBranchLength}), ставим тупик у {end.fromRoom.name}");
                // ExpandFromEnd(end, RoomKind.DeadEnd, enqueueExits: false, countAsRoom: false);
                break;
            }
            RoomKind nextKind;
            if (!portalPlaced && spawnedRooms.Count >= portalTarget)
            {
                nextKind = RoomKind.Portal;
            }
            else
            {
                nextKind = RollAnyKind();
                if (corSeqCount >= 2 && nextKind == RoomKind.Corridor)
                    nextKind = RollAnyKindExceptCorridor();
                if (end.fromRoom.kind == RoomKind.Triple)
                    nextKind = RollAnyKindExceptTriple();
            }

            bool canPlace =
                nextKind == RoomKind.Triple ? CanPlaceTripleWithDeadEndsAll3(end) :
                nextKind == RoomKind.Single ? CanPlaceSingleWithDeadEnd(end) :
                nextKind == RoomKind.Corridor ? CanPlaceCorridorThenDeadEnd(end) :
                nextKind == RoomKind.Portal ? CanPlaceTripleWithDeadEndsAll3(end) :
                true;

            if (!canPlace)
            {
                Debug.Log($"[BUILD] check FAILED for {nextKind}, place DeadEnd");
                // ExpandFromEnd(end, RoomKind.DeadEnd, enqueueExits: false, countAsRoom: false);
                corSeqCount = 0;
                continue;
            }
            if (nextKind == RoomKind.Corridor)
            {
                corSeqCount++;
            }
            Debug.Log($"[BUILD] check OK for {nextKind}");
            // **ExpandFromEnd теперь должен добавить новые выходы в sideFrontier**
            
            ExpandFromEndMain(end, nextKind, enqueueExits: true, countAsRoom: true, mainFrontier: mainFrontier, sideFrontier: sideFrontier);
            if (nextKind == RoomKind.Corridor) corSeqCount++; else corSeqCount = 0;
            if (nextKind == RoomKind.Portal) portalPlaced = true;
        }
        if (mainFrontier.Count == 0)
        Debug.Log("[BUILD][EXIT] Причина: mainFrontier пустой, больше нечего строить по основным веткам.");
        else if (spawnedRooms.Count >= maxRooms)
            Debug.Log("[BUILD][EXIT] Причина: достигнут лимит комнат maxRooms.");
        else if (safety <= 0)
            Debug.Log("[BUILD][EXIT] Причина: достигнут лимит safety — возможная ошибка или бесконечный цикл.");

        // 3) После главной ветки — остальные ветки
        safety = 100000;
        while (sideFrontier.Count > 0 && spawnedRooms.Count < maxRooms && safety-- > 0)
        {
            // while (sideFrontier.Count > 0)
            // {
            //     OpenEnd item = sideFrontier.Dequeue();
            //     Build1(item);
            // }
            OpenEnd end = sideFrontier.Dequeue();
            string roomName = end.fromRoom != null ? end.fromRoom.name : "null";
            string doorType = end.fromDoor != null ? end.fromDoor.sideType.ToString() : "null";
            string doorName = end.fromDoor != null ? end.fromDoor.name : "null";
            Debug.Log($"[SIDEFRONTIER] НАЧАЛО: room={roomName}, door={doorName}, side={doorType}, branchLength={end.branchLength}");
            Debug.Log($"[DBG] SidePath длина {end.branchLength}, лимит {maxBranchLength}");

            RoomKind nextKind;
            if (!portalPlaced && spawnedRooms.Count >= portalTarget)
            {
                nextKind = RoomKind.Portal;
            }
            else
            {
                nextKind = RollAnyKind();
                if (corSeqCount >= 2 && nextKind == RoomKind.Corridor)
                    nextKind = RollAnyKindExceptCorridor();
                if (end.fromRoom.kind == RoomKind.Triple)
                    nextKind = RollAnyKindExceptTriple();
                Debug.Log($"[ROLLED] {nextKind}");
            }

            bool canPlace =
                nextKind == RoomKind.Triple ? CanPlaceTripleWithDeadEndsAll3(end) :
                nextKind == RoomKind.Single ? CanPlaceSingleWithDeadEnd(end) :
                nextKind == RoomKind.Corridor ? CanPlaceCorridorThenDeadEnd(end) :
                nextKind == RoomKind.Portal ? CanPlaceSingleWithDeadEnd(end) :
                true;

            if (!canPlace)
            {
                Debug.Log($"[BUILD] check FAILED for {nextKind}, place DeadEnd");
                // ExpandFromEnd(end, RoomKind.DeadEnd, enqueueExits: false, countAsRoom: false, mainFrontier: null, sideFrontier: sideFrontier);
                corSeqCount = 0;
                continue;
            }
            if (nextKind == RoomKind.Corridor)
            {
                corSeqCount++;
            }

            Debug.Log($"[BUILD] check OK for {nextKind}");
            ExpandFromEnd(end, nextKind, enqueueExits: true, countAsRoom: true, mainFrontier: null, sideFrontier: sideFrontier);
            if (nextKind == RoomKind.Corridor) corSeqCount++; else corSeqCount = 0;
            if (nextKind == RoomKind.Portal) portalPlaced = true;

            Debug.Log($"[SIDEFRONTIER] Текущий состав очереди sideFrontier (Count={sideFrontier.Count}):");
                foreach (var end1 in sideFrontier)
                {
                    string roomName1 = end1.fromRoom != null ? end1.fromRoom.name : "null";
                    string doorType1 = end1.fromDoor != null ? end1.fromDoor.sideType.ToString() : "null";
                    string doorName1 = end1.fromDoor != null ? end1.fromDoor.name : "null";
                    Debug.Log($"[SIDEFRONTIER] room={roomName1}, door={doorName1}, side={doorType1}, branchLength={end.branchLength}");
                }
        }

        // 4) Если портал не поставлен — форсируем на первой доступной ветке
        if (!portalPlaced && sideFrontier.Count > 0 && spawnedRooms.Count < maxRooms)
        {
            var end = sideFrontier.Dequeue();
            bool canPlacePortal = CanPlacePortalWithDeadEndsAll3(end);
            if (canPlacePortal)
            {
                ExpandFromEnd(end, RoomKind.Portal, enqueueExits: true, countAsRoom: true, mainFrontier: null, sideFrontier: sideFrontier);
                portalPlaced = true;
            }
            else
            {
                ExpandFromEnd(end, RoomKind.DeadEnd, enqueueExits: false, countAsRoom: false, mainFrontier: null, sideFrontier: sideFrontier);
            }
        }

        // Собираем все выжившие "временные" тупики в общий список
        foreach (var pending in pendingDeadEnds.Values)
        {
            if (pending.instance != null && !spawnedRooms.Contains(pending.instance))
            {
                spawnedRooms.Add(pending.instance);
            }
        }

        Debug.Log($"[BUILD] Done. rooms={spawnedRooms.Count}, portalPlaced={portalPlaced}, seed={config.seed}");
        var vis = FindFirstObjectByType<DungeonVisibilityManager>();
        if (vis != null) vis.RegisterRooms(spawnedRooms);
    }

    private RoomKind RollAnyKindExceptCorridor()
    {
        double r = rng.NextDouble();
        if (r < 0.60) return RoomKind.Single;   // single 60%
        else return RoomKind.Triple;            // triple 40%
    }

    private RoomKind RollAnyKindExceptTriple()
        {
            double r = rng.NextDouble();
            if (r < 0.60) return RoomKind.Corridor;   // single 60%
            else return RoomKind.Single; 
        }

    private bool CanPlacePortalWithDeadEndsAll3(OpenEnd end)
    {
        // Просто используй ту же логику, что для Triple, но с prefab.portalRoom
        // (можно скопировать CanPlaceTripleWithDeadEndsAll3, сменить prefabs.tripleRoom на prefabs.portalRoom)
        // Если надо — дай знать, дам полностью этот метод!
        return true; // пока что для теста
    }

    private void ExpandFromEnd(OpenEnd end, RoomKind kind, bool enqueueExits = true, bool countAsRoom = true, Queue<OpenEnd> mainFrontier = null, Queue<OpenEnd> sideFrontier = null)
    {
        if (end == null || end.fromRoom == null || end.fromDoor == null) return;

        var prefab = PickPrefab(kind);
        if (prefab == null) return;

        Vector3 dir = end.fromDoor.SideDir;
        if (dir.sqrMagnitude < 0.0001f) dir = end.fromRoom.transform.forward;
        dir.y = 0f;
        dir.Normalize();

        NodeInstance node = Instantiate(prefab, Vector3.zero, Quaternion.identity, GetRoot());
        node.kind = kind;
        node.name = $"{kind}_{nodeIndex++}";

        AlignByAttach(node, end.fromDoor.SocketPos + dir * GetRoomOffset(kind), dir);

        if (end.fromRoom != null)
        {
            end.fromRoom.AddNeighbor(node);
            node.AddNeighbor(end.fromRoom);
        }

        // Добавляем выходы только в sideFrontier!
        if (enqueueExits && sideFrontier != null)
            EnqueueLogicalExits(node, kind, end.branchLength, sideFrontier);

        if (countAsRoom && kind != RoomKind.Corridor)
            spawnedRooms.Add(node);
        else if (countAsRoom && kind == RoomKind.Corridor)
            spawnedRooms.Add(node);

        if (kind == RoomKind.Portal)
            portalPlaced = true;

        Debug.Log($"[CONNECT] {end.fromRoom.name} -> {node.name}");

        if (kind != RoomKind.DeadEnd)
        {
            List<DoorPortal> allDoors = GetAllDoors(node);
            foreach (var door in allDoors)
            {
                Vector3 ddir = door.SideDir;
                if (ddir.sqrMagnitude < 0.0001f) ddir = node.transform.forward;
                ddir.y = 0f;
                ddir.Normalize();
                RegisterPendingDeadEnd(door, ddir);
            }
        }
        Physics.SyncTransforms();
    }

    private void ExpandFromEndMain(OpenEnd end, RoomKind kind, bool enqueueExits = true, bool countAsRoom = true, Queue<OpenEnd> mainFrontier = null, Queue<OpenEnd> sideFrontier = null)
    {
        if (end == null || end.fromRoom == null || end.fromDoor == null) return;

        var prefab = PickPrefab(kind);
        if (prefab == null) return;

        Vector3 dir = end.fromDoor.SideDir;
        if (dir.sqrMagnitude < 0.0001f) dir = end.fromRoom.transform.forward;
        dir.y = 0f;
        dir.Normalize();

        NodeInstance node = Instantiate(prefab, Vector3.zero, Quaternion.identity, GetRoot());
        node.kind = kind;
        node.name = $"{kind}_{nodeIndex++}";

        AlignByAttach(node, end.fromDoor.SocketPos + dir * GetRoomOffset(kind), dir);

        DoorPortal nodeDoor = GetForwardDoor(node);

        // <--- ИСПРАВЛЕНИЕ: Соседство ставим безусловно --->
        if (end.fromRoom != null)
        {
            end.fromRoom.AddNeighbor(node);
            node.AddNeighbor(end.fromRoom);
        }
        // Перед использованием end, fromRoom, fromDoor, node, nodeDoor, mainFrontier:
        if (end == null)
            Debug.LogError("[ExpandFromEndMain] end == null!");
        else if (end.fromRoom == null)
            Debug.LogError($"[ExpandFromEndMain] end.fromRoom == null! end={end}");
        else if (end.fromDoor == null)
            Debug.LogError($"[ExpandFromEndMain] end.fromDoor == null! Room={end.fromRoom?.name}");
        else if (prefab == null)
            Debug.LogError($"[ExpandFromEndMain] prefab == null! kind={kind}");
        else if (node == null)
            Debug.LogError($"[ExpandFromEndMain] node == null after Instantiate!");
        else if (node.portals == null)
            Debug.LogError($"[ExpandFromEndMain] node.portals == null! node={node.name}");
        else if (mainFrontier == null)
            Debug.LogError($"[ExpandFromEndMain] mainFrontier == null!");
        else if (nodeDoor == null)
            Debug.LogError($"[ExpandFromEndMain] nodeDoor == null! node={node.name}, portals={node.portals?.Length}");

        if (end == null || end.fromRoom == null || end.fromDoor == null || prefab == null || node == null || node.portals == null || mainFrontier == null || nodeDoor == null)
        {
            Debug.LogError("[ExpandFromEndMain] ОПАСНО: какой-то из объектов == null, дальнейшее выполнение прекращено!");
            return;
        }

        mainFrontier.Enqueue(new OpenEnd {
                fromRoom = node,
                fromDoor = nodeDoor,
                branchLength = end.branchLength + 1
            });
        
        if (enqueueExits && sideFrontier != null)
            EnqueueLogicalExitsLeftRight(node, kind, 0, sideFrontier);

        if (countAsRoom && kind != RoomKind.Corridor)
            spawnedRooms.Add(node);
        else if (countAsRoom && kind == RoomKind.Corridor)
            spawnedRooms.Add(node);

        if (kind == RoomKind.Portal)
            portalPlaced = true;

        Debug.Log($"[CONNECT] {end.fromRoom.name} -> {node.name}");
        if (kind != RoomKind.DeadEnd)
        {
            List<DoorPortal> allDoors = GetAllDoors(node);
            foreach (var door in allDoors)
            {
                Vector3 ddir = door.SideDir;
                if (ddir.sqrMagnitude < 0.0001f) ddir = node.transform.forward;
                ddir.y = 0f;
                ddir.Normalize();
                RegisterPendingDeadEnd(door, ddir);
            }
        }
        Physics.SyncTransforms();
    }

    private float GetRoomOffset(RoomKind kind)
    {
        // Нужно подкорректировать значения под твои префабы!
        switch (kind)
        {
            case RoomKind.Corridor: return roomToCorridorOffset; // corridor
            case RoomKind.DeadEnd:  return roomToCorridorOffset; // deadend сразу к двери
            default: return roomToCorridorOffset; // или твой corridorToRoomOffset для обычных комнат если нужно больше
        }
    }

    private void EnqueueLogicalExits(NodeInstance room, RoomKind kind, int branchLength, Queue<OpenEnd> targetFrontier)
    {
        Debug.Log($"[EnqueueLogicalExits] {room} {kind}");
        int exits = GetLogicalExitCount(kind);
        if (exits <= 0) return;

        List<DoorPortal> doors = GetAllDoors(room);
        if (doors.Count == 0) return;

        // Для Triple все выходы — ветки!
        for (int i = 0; i < Mathf.Min(exits, doors.Count); i++)
        {
            targetFrontier.Enqueue(new OpenEnd {
                fromRoom = room,
                fromDoor = doors[i],
                branchLength = branchLength + 1
            });
        }
    }

    private void EnqueueLogicalExitsLeftRight(NodeInstance room, RoomKind kind, int branchLength, Queue<OpenEnd> targetFrontier)
    {
        int exits = GetLogicalExitCount(kind);
        if (exits <= 0) return;

        List<DoorPortal> doors = GetAllDoors(room);
        if (doors.Count == 0) return;

        if (kind == RoomKind.Triple)
        {
            // Только Left и Right
            foreach (var door in doors)
            {
                if (door == null) continue;
                if (door.sideType == DoorSideType.Left || door.sideType == DoorSideType.Right)
                {
                    targetFrontier.Enqueue(new OpenEnd {
                        fromRoom = room,
                        fromDoor = door,
                        branchLength = branchLength
                    });
                }
            }
        }
    }
    // По твоим правилам:
    // DeadEnd=0, Single=1, Start=1, Portal=3, Triple=3
    private int GetLogicalExitCount(RoomKind kind)
    {
        return kind switch
        {
            RoomKind.DeadEnd => 0,
            RoomKind.Single => 1,
            RoomKind.Start => 1,
            RoomKind.Portal => 3,
            RoomKind.Triple => 3,
            _ => 1
        };
    }

    private NodeInstance SpawnRoom(NodeInstance prefab, RoomKind kind, Vector3 pos, Quaternion rot)
    {
        NodeInstance n = Instantiate(prefab, pos, rot, GetRoot());
        n.kind = kind;
        n.name = $"{kind}_{nodeIndex++}";
        spawnedRooms.Add(n);

        Debug.Log($"[PLACE] {n.name} kind={kind} portals={(n.portals == null ? 0 : n.portals.Length)}");
        return n;
    }

    private DoorPortal GetAnyDoor(NodeInstance n)
    {
        if (n == null || n.portals == null) return null;
        foreach (var p in n.portals)
            if (p != null) return p;
        return null;
    }

    private DoorPortal GetForwardDoor(NodeInstance n)
    {
        if (n == null || n.portals == null) return null;
        foreach (var p in n.portals)
        {
            if (p != null && p.sideType == DoorSideType.Forward)
                return p;
        }
        return null;
    }

    private List<DoorPortal> GetAllDoors(NodeInstance n)
    {
        var list = new List<DoorPortal>();
        if (n == null || n.portals == null) return list;
        foreach (var p in n.portals)
            if (p != null) list.Add(p);
        return list;
    }

    private void AlignByAttach(NodeInstance inst, Vector3 targetPos, Vector3 targetForward)
    {
        Transform r = inst.transform;
        Transform a = inst.attachFromSide ? inst.attachFromSide : r;

        Vector3 af = a.forward; af.y = 0f;
        Vector3 tf = targetForward; tf.y = 0f;
        if (af.sqrMagnitude < 0.0001f || tf.sqrMagnitude < 0.0001f) return;

        af.Normalize();
        tf.Normalize();

        float ay = Mathf.Atan2(af.x, af.z) * Mathf.Rad2Deg;
        float ty = Mathf.Atan2(tf.x, tf.z) * Mathf.Rad2Deg;
        float dy = Mathf.DeltaAngle(ay, ty);

        r.Rotate(0f, dy, 0f, Space.World);
        r.position += (targetPos - a.position);

        Vector3 e = r.eulerAngles;
        r.rotation = Quaternion.Euler(0f, e.y, 0f);
    }

    private NodeInstance PickPrefab(RoomKind kind)
    {
        return kind switch
        {
            RoomKind.Start => prefabs.startRoom,
            RoomKind.Single => prefabs.singleRoom,
            RoomKind.Triple => prefabs.tripleRoom,
            RoomKind.DeadEnd => prefabs.deadEndRoom,
            RoomKind.Portal => prefabs.portalRoom,
            RoomKind.Corridor => prefabs.corridor,
            _ => prefabs.singleRoom
        };
    }

    private bool ValidateRefs()
    {
        if (config == null || prefabs == null)
        {
            Debug.LogError("[BUILD] config/prefabs null");
            return false;
        }

        if (prefabs.startRoom == null || prefabs.singleRoom == null || prefabs.tripleRoom == null ||
            prefabs.deadEndRoom == null || prefabs.portalRoom == null || prefabs.corridor == null)
        {
            Debug.LogError("[BUILD] assign all room prefabs");
            return false;
        }

        return true;
    }

    private Transform GetRoot() => root ? root : transform;

    private void ClearChildren()
    {
        frontier.Clear();
        spawnedRooms.Clear();
        pendingDeadEnds.Clear(); // ← добавить

        Transform r = GetRoot();
        for (int i = r.childCount - 1; i >= 0; i--)
            DestroyImmediate(r.GetChild(i).gameObject);
    }
}