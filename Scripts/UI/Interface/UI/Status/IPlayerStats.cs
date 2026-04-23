public interface IPlayerStats
{
    int Level { get; }
    int Experience { get; }
    int ExperienceToNextLevel { get; }
    int Gold { get; set; }
    int SkillPoints { get; set; }
    int AttributePoints { get; set; }

    int Strength { get; set; }
    int Agility { get; set; }
    int Intelligence { get; set; }
    int Endurance { get; set; }
    int Perception { get; set; }
    int Resistance { get; set; }
    int Luck { get; set; }

    float MaxHP { get; }
    float CurrentHP { get; }
    float MaxMP { get; }
    float CurrentMP { get; }
    float Fatigue { get; }
    float HpRegen { get; }
    float MpRegen { get; }
    float AtkRange { get; }
    float AtkSpeed { get; }
    float CriticalChance { get; }
    float CritCooldown { get; }

    void TakeDamage(float damage, string damageType);
    float CalculateDamage(bool isCritical);
    void ResetAttributes();
}