//
// RoomPrefabSet is a serializable data container that groups all dungeon room prefabs
// in one place so DungeonBuilder can look up the correct NodeInstance prefab for
// each RoomKind without scattered Inspector references.
//

using UnityEngine;

[System.Serializable]
public class RoomPrefabSet
{
    public NodeInstance startRoom;
    public NodeInstance singleRoom;
    public NodeInstance tripleRoom;
    public NodeInstance deadEndRoom;
    public NodeInstance portalRoom;
    public NodeInstance corridor;
}
