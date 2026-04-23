using UnityEngine;

public class DungeonBootstrap : MonoBehaviour
{
    [SerializeField] private DungeonBuilder builder;

    private void Start()
    {
        if (builder != null) builder.Build();
    }
}