//
// DungeonConfig is a ScriptableObject that holds the configuration parameters
// for dungeon generation: the RNG seed and the maximum number of rooms to place.
//

using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Config")]
public class DungeonConfig : ScriptableObject
{
    public int seed = 12345;
    public int maxRooms = 30;
}
