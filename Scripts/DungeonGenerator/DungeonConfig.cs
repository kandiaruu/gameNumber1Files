using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Config")]
public class DungeonConfig : ScriptableObject
{
    public int seed = 12345;
    public int maxRooms = 30;
}