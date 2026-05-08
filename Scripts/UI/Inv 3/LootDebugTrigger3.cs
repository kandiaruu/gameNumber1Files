//
// Debug utility that triggers loot events via keyboard input. Pressing the 1 key
// simulates a goblin kill by calling AddGoblinKill on the injected ILootManager3,
// allowing loot flow to be tested without actual gameplay.
//

using UnityEngine;

public class LootDebugTrigger3 : MonoBehaviour
{
    [InjectAttribute1] private ILootManager3 lootManager3 { get; set; }

    // Resolves injected dependencies when the object is first created
    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    // Listens for the 1 key each frame and fires a goblin kill loot event when pressed
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            lootManager3.AddGoblinKill();
        }
    }
}
