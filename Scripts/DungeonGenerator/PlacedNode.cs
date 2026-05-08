//
// PlacedNode is a lightweight data class that pairs a logical DungeonNode graph entry
// with its instantiated scene NodeInstance and the 2D grid cell it occupies,
// used internally by the dungeon generator to track placed rooms.
//

using UnityEngine;

public class PlacedNode
{
    public DungeonNode graph;
    public NodeInstance view;
    public Vector2Int cell;
}
