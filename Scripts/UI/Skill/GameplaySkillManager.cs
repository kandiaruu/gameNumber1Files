//
// Manages gameplay mechanics for skills including damage calculations and effect application
//

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public interface IGameplaySkillManager
{
    float CalculateFinalDamage(float baseDamage, List<string> attackTags);
    void ApplyOnHitEffects(EnemyAI target, List<string> attackTags);
    List<Skill> GetUnlockedSkillsByCategory(SkillCategory category);
}

public class GameplaySkillManager : MonoBehaviour, IGameplaySkillManager
{
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; }
    private DamagePopupSpawner damagePopupSpawner;

    //
    // Initializes dependencies and finds the damage popup spawner
    //
    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        damagePopupSpawner = FindFirstObjectByType<DamagePopupSpawner>();
    }

    //
    // Calculates final damage by applying passive skill bonuses using formula: (Base + Flat) * (1 + Percent) * (1 + MaxPercent)
    //
    public float CalculateFinalDamage(float baseDamage, List<string> attackTags)
    {
        float flatBonus = 0f;
        float percentBonus = 0f;
        float maxPercentBonus = 0f;

        if (skillTreeManager == null || attackTags == null || attackTags.Count == 0) return baseDamage;

        var passives = GetUnlockedSkillsByCategory(SkillCategory.Passive);

        foreach (var passive in passives)
        {
            if (passive.tags != null && passive.tags.Intersect(attackTags).Any())
            {
                int levelIndex = Mathf.Max(0, passive.currentLevel - 1);
                if (passive.levelStats != null && passive.levelStats.Length > levelIndex)
                {
                    SkillLevelData stats = passive.levelStats[levelIndex];
                    flatBonus += stats.flatDamageBonus;
                    percentBonus += stats.percentDamageBonus;
                    maxPercentBonus += stats.maxPercentDamageBonus;
                }
            }
        }

        float damageWithFlat = baseDamage + flatBonus;
        float damageWithPercent = damageWithFlat * (1f + percentBonus);
        float finalDamage = damageWithPercent * (1f + maxPercentBonus);

        return finalDamage;
    }

    //
    // Applies on-hit effects from passive skills to the target, such as burn damage over time
    //
    public void ApplyOnHitEffects(EnemyAI target, List<string> attackTags)
    {
        if (target == null || !target.IsAlive || skillTreeManager == null || attackTags == null) return;

        var passives = GetUnlockedSkillsByCategory(SkillCategory.Passive);
        foreach (var passive in passives)
        {
            if (passive.tags == null || passive.levelStats == null) continue;

            int levelIndex = Mathf.Max(0, passive.currentLevel - 1);
            if (passive.levelStats.Length <= levelIndex) continue;

            SkillLevelData stats = passive.levelStats[levelIndex];

            if (stats.effectDuration > 0 && passive.tags.Contains("Fire") && attackTags.Contains("Fire"))
            {
                StartCoroutine(BurnRoutine(target, stats.effectDamage, stats.effectDuration));
            }
        }
    }

    //
    // Applies continuous burn damage to a target over a duration
    //
    private IEnumerator BurnRoutine(EnemyAI target, float damageTick, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && target != null && target.IsAlive)
        {
            yield return new WaitForSeconds(1f);
            if (target != null && target.IsAlive)
            {
                target.TakeDamage(damageTick);
                if (damagePopupSpawner != null)
                {
                    Color orangeColor = new Color(1f, 0.5f, 0f);
                    // damagePopupSpawner.ShowMessage(target.transform, damageTick.ToString(), orangeColor);
                }
            }
            elapsed += 1f;
        }
    }

    //
    // Returns all unlocked skills of a specific category
    //
    public List<Skill> GetUnlockedSkillsByCategory(SkillCategory category)
    {
        if (skillTreeManager == null) return new List<Skill>();
        return skillTreeManager.GetAllSkills().Where(s => s.isUnlocked && s.category == category).ToList();
    }
}
