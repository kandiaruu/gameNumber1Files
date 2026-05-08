//
// CritPointMarker is attached to a weak-spot object on an enemy.
// When the spot is hit it notifies the owning EnemyAI and destroys itself.
//

using UnityEngine;

public class CritPointMarker : MonoBehaviour
{
    [HideInInspector] public EnemyAI owner;

    // Notifies the owning enemy that this crit point was struck, then destroys the weak-spot GameObject
    public void OnCritHit()
    {
        if (owner != null)
            owner.OnCritPointDestroyed();

        Destroy(gameObject);
    }
}
