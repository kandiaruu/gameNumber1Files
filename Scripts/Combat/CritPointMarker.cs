using UnityEngine;

public class CritPointMarker : MonoBehaviour
{
    [HideInInspector] public EnemyAI owner;

    // Вызывается при попадании по этой точке
    public void OnCritHit()
    {
        if (owner != null)
            owner.OnCritPointDestroyed();

        Destroy(gameObject);
    }
}