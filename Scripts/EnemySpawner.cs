using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint; // Можно задать точку спавна в инспекторе
    [InjectAttribute1] public IPlayerStats playerStats { get; set; }

    private void Start()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        // Если не задана точка спавна — спавним у спавнера
        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        GameObject enemyObj = Instantiate(enemyPrefab, pos, Quaternion.identity);

        }
}