using System.Collections.Generic;

public class DungeonNode
{
    public int id;
    public RoomKind kind;
    public List<DungeonNode> links = new();
    public int doorCapacity; // Single=1, Triple=3, DeadEnd=0, Portal=1(или 3 - как решишь)

    public int FreeDoors => doorCapacity - links.Count;
}