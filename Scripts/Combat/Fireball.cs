//
// Fireball is a projectile that travels forward at a speed defined by its current skill level stats.
// On collision it deals pre-calculated damage to enemies or their crit points, shows a damage popup,
// triggers on-hit gameplay effects, plays a 2D hit sound, and destroys itself.
// Player colliders and trigger volumes are ignored.
//

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ActiveSkillStats
{
    public float damage = 100f;
    public float manaCost = 20f;
    public float cooldown = 2f;
    public float speed = 20f;
}

public class Fireball : MonoBehaviour
{
    [Header("Stats per level (index 0 = level 1)")]
    public ActiveSkillStats[] statsPerLevel;

    [Header("Flight settings")]
    public float maxDistance = 500f;
    public AudioClip hitSound;

    [HideInInspector] public float calculatedDamage = 0f;
    [HideInInspector] public int currentSkillLevel = 1;
    [HideInInspector] public List<string> skillTags;
    [HideInInspector] public IGameplaySkillManager gameplayManager;

    private Vector3 startPosition;
    private DamagePopupSpawner damagePopupSpawner;
    private ActiveSkillStats myStats;

    // Records the spawn position, locates the DamagePopupSpawner in the scene, and selects the stat block for the current skill level
    private void Start()
    {
        startPosition = transform.position;
        damagePopupSpawner = FindFirstObjectByType<DamagePopupSpawner>();

        int index = Mathf.Max(0, currentSkillLevel - 1);
        if (statsPerLevel != null && statsPerLevel.Length > 0)
        {
            index = Mathf.Min(index, statsPerLevel.Length - 1);
            myStats = statsPerLevel[index];
        }
        else
        {
            myStats = new ActiveSkillStats();
        }
    }

    // Moves the projectile forward each frame at the stat-defined speed and destroys it once it exceeds maxDistance
    private void Update()
    {
        if (myStats == null) return;

        transform.Translate(Vector3.forward * myStats.speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            Destroy(gameObject);
    }

    // Handles collisions: ignores the player and triggers, applies damage and on-hit effects to enemies or crit points, plays a hit sound, then destroys the projectile
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<ThirdPersonCharacter>() != null)
            return;

        if (other.isTrigger)
            return;

        var enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(calculatedDamage);

            if (damagePopupSpawner != null)
                damagePopupSpawner.ShowDamage(enemy.transform, calculatedDamage, false, DamageType.Magical);

            if (gameplayManager != null && skillTags != null)
                gameplayManager.ApplyOnHitEffects(enemy, skillTags);
        }
        else
        {
            var critPoint = other.GetComponent<CritPointMarker>();
            if (critPoint != null && critPoint.owner != null)
            {
                critPoint.owner.TakeDamage(calculatedDamage);
                if (damagePopupSpawner != null)
                {
                    damagePopupSpawner.ShowDamage(critPoint.owner.transform, calculatedDamage, true, DamageType.Magical);
                }

                if (gameplayManager != null && skillTags != null)
                    gameplayManager.ApplyOnHitEffects(critPoint.owner, skillTags);
            }
        }

        if (hitSound != null)
        {
            GameObject audioObj = new GameObject("FireballHitSound");
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = hitSound;
            source.spatialBlend = 0f;
            source.Play();
            Destroy(audioObj, hitSound.length);
        }

        Destroy(gameObject);
    }
}
