using UnityEngine;

public class LootDebugTrigger3 : MonoBehaviour
{
    [InjectAttribute1] private ILootManager3 lootManager3 { get; set; }

    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            lootManager3.AddGoblinKill();
        }
    }
}