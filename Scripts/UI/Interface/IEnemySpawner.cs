using UnityEngine;

public interface IEnemySpawner
{
    // Теперь метод принимает и точку, и будущего родителя
    void SpawnEnemyAt(Transform point, Transform parent);
}