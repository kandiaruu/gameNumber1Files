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

    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        damagePopupSpawner = FindFirstObjectByType<DamagePopupSpawner>();
    }

    // 1. Считаем итоговый урон по формуле
    public float CalculateFinalDamage(float baseDamage, List<string> attackTags)
    {
        float flatBonus = 0f;
        float percentBonus = 0f;
        float maxPercentBonus = 0f;

        if (skillTreeManager == null || attackTags == null || attackTags.Count == 0) return baseDamage;

        // Берем все пассивки
        var passives = GetUnlockedSkillsByCategory(SkillCategory.Passive);

        foreach (var passive in passives)
        {
            // Если теги совпадают (например, атака Fire и пассивка Fire)
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

        // ФОРМУЛА: (Базовый + Плоский) * (1 + Процентный) * (1 + Макс.Процентный)
        float damageWithFlat = baseDamage + flatBonus;
        float damageWithPercent = damageWithFlat * (1f + percentBonus);
        float finalDamage = damageWithPercent * (1f + maxPercentBonus);

        return finalDamage;
    }

    // 2. Накладываем эффекты
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

            // Если есть длительность эффекта, значит это ДоТ (урон со временем)
            if (stats.effectDuration > 0 && passive.tags.Contains("Fire") && attackTags.Contains("Fire"))
            {
                StartCoroutine(BurnRoutine(target, stats.effectDamage, stats.effectDuration));
            }
        }
    }

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
                    damagePopupSpawner.ShowMessage(target.transform, damageTick.ToString(), orangeColor);
                }
            }
            elapsed += 1f;
        }
    }

    public List<Skill> GetUnlockedSkillsByCategory(SkillCategory category)
    {
        if (skillTreeManager == null) return new List<Skill>();
        return skillTreeManager.GetAllSkills().Where(s => s.isUnlocked && s.category == category).ToList();
    }
}