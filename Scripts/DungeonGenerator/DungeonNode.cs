//
// DungeonNode is a pure data class representing one node in the dungeon's logical graph.
// It tracks the node's kind, its connections to other nodes, and how many free door
// slots remain for the generator to attach further rooms.
//

using System.Collections.Generic;

public class DungeonNode
{
    public int id;
    public RoomKind kind;
    public List<DungeonNode> links = new();
    public int doorCapacity;

    // Returns the number of doors not yet connected to another node
    public int FreeDoors => doorCapacity - links.Count;
}
