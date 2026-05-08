using UnityEngine;

public interface IEnemySpawner
{
    void SpawnEnemyAt(Transform point, Transform parent);
}