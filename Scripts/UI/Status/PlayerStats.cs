using UnityEngine;

//
// Stores and manages all player statistics: level, experience, resources (HP/MP/fatigue),
// primary attributes (Strength, Agility, etc.), combat modifiers, and regeneration.
// Handles leveling up, attribute resets, damage intake, and death/respawn logic.
//

public class PlayerStats : MonoBehaviour, IPlayerStats
{
    [SerializeField] private int level = 1;
    [SerializeField] private int experience = 0;
    [SerializeField] private int experienceToNextLevel = 100;
    [SerializeField] private int gold = 0;
    [SerializeField] private int skillPoints = 0;
    [SerializeField] private int attributePoints = 0;

    [SerializeField] private int strength = 5;
    [SerializeField] private int agility = 5;
    [SerializeField] private int endurance = 5;
    [SerializeField] private int perception = 0;
    [SerializeField] private int intelligence = 0;
    [SerializeField] private int resistance = 0;
    [SerializeField] private int luck = 0;

    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float baseHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float maxMP = 50f;
    [SerializeField] private float baseMP = 50f;
    [SerializeField] private float currentMP = 50f;
    [SerializeField] private float fatigue = 0f;

    [Header("Combat Stats")]
    [SerializeField] private float criticalChance = 0f;
    [SerializeField] private float criticalDamage = 25f;
    [SerializeField] private float critCooldown = 5f;
    [SerializeField] private float physicalArmor = 0f;
    [SerializeField] private float magicArmor = 0f;
    [SerializeField] private float dodgeChance = 0f;
    [SerializeField] private float damageResistance = 0f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deathMusic;

    [Header("Regen Stats")]
    [SerializeField] private float hpRegen = 1f;
    [SerializeField] private float mpRegen = 1f;

    [Header("Other")]
    [SerializeField] private float atkSpeed = 2f;
    [SerializeField] private float atkRange = 2f;

    [InjectAttribute1] private IDungeonFloorManager floorManager { get; set; }

    public float AtkRange => atkRange;
    public float AtkSpeed => atkSpeed;
    public float CriticalChance => criticalChance;
    public float CritCooldown => critCooldown;

    private float regenTimer = 0f;
    public int Level => level;
    public int Experience => experience;
    public int ExperienceToNextLevel => experienceToNextLevel;

    public int AttributePoints
    {
        get => attributePoints;
        set => attributePoints = value;
    }

    public int SkillPoints
    {
        get => skillPoints;
        set => skillPoints = value;
    }

    public int Gold
    {
        get => gold;
        set => gold = value;
    }

    public int Strength
    {
        get => strength;
        set => strength = value;
    }

    public int Agility
    {
        get => agility;
        set => agility = value;
    }

    public int Intelligence
    {
        get => intelligence;
        set => intelligence = value;
    }

    public int Endurance
    {
        get => endurance;
        set => endurance = value;
    }

    public int Perception
    {
        get => perception;
        set => perception = value;
    }

    public int Resistance
    {
        get => resistance;
        set => resistance = value;
    }

    public int Luck
    {
        get => luck;
        set => luck = value;
    }

    public float MaxHP => Round1(maxHP);
    public float CurrentHP => Round1(currentHP);
    public float MaxMP => Round1(maxMP);
    public float CurrentMP => Round1(currentMP);
    public float Fatigue => fatigue;

    // Increases the player's level, raises base HP/MP and core attributes, and recalculates the experience threshold
    public void LevelUp()
    {
        level++;

        attributePoints += 1;
        skillPoints += 1;

        strength += 1;
        agility += 1;
        endurance += 1;

        baseHP += 100f;
        baseMP += 50f;

        experienceToNextLevel = (int)(100 * Mathf.Pow(1.1f, level - 1));
    }

    // Recalculates derived max HP/MP and regen rates from base values and attributes
    public void updateHM()
    {
        maxHP = baseHP + (baseHP * 0.01f * resistance);
        maxMP = baseMP + (baseMP * 0.001f * intelligence);
        hpRegen = 1f + (baseHP * 0.0005f * resistance);
        mpRegen = 1f + (baseMP * 0.001f * intelligence);
    }

    public float HpRegen
    {
        get => hpRegen;
        set => hpRegen = value;
    }

    public float MpRegen
    {
        get => mpRegen;
        set => mpRegen = value;
    }

    // Refunds all attribute points spent above the base value and resets all attributes to their level-based default
    public void ResetAttributes()
    {
        int baseValue = 5 + (level - 1);

        int refundedPoints = 0;

        refundedPoints += (strength - baseValue);
        refundedPoints += (agility - baseValue);
        refundedPoints += (endurance - baseValue);

        strength = baseValue;
        agility = baseValue;
        endurance = baseValue;

        refundedPoints += intelligence;
        refundedPoints += perception;
        refundedPoints += resistance;
        refundedPoints += luck;

        intelligence = 0;
        perception = 0;
        resistance = 0;
        luck = 0;

        attributePoints += refundedPoints;

        updateHM();
    }

    // Rounds a float value down to one decimal place
    private float Round1(float value)
    {
        return Mathf.Floor(value * 10f) / 10f;
    }

    // Sets current HP clamped to [0, maxHP], rounded to one decimal place
    private void SetHP(float value)
    {
        currentHP = Mathf.Clamp(Round1(value), 0f, maxHP);
    }

    // Sets current MP clamped to [0, maxMP], rounded to one decimal place
    private void SetMP(float value)
    {
        currentMP = Mathf.Clamp(Round1(value), 0f, maxMP);
    }

    // Calculates base physical damage from Strength and level, optionally applying the critical damage bonus
    public float CalculateDamage(bool isCrit)
    {
        float baseDamage = strength * 2f;

        baseDamage *= 1f + (level * 0.1f);

        float finalDamage = baseDamage;

        if (isCrit == true)
        {
            finalDamage += baseDamage * (criticalDamage / 100f);
        }

        finalDamage = Mathf.Floor(finalDamage * 10f) / 10f;
        return finalDamage;
    }

    // Applies incoming damage of a given type, factoring in dodge, armor, and damage resistance
    public void TakeDamage(float damage, string damageType)
    {
        float finalDamage = Mathf.Floor(damage * 10f) / 10f;
        Debug.Log($"Taking {finalDamage}");

        if (damageType.ToLower() == "absolute")
        {
            SetHP(currentHP - finalDamage);
            return;
        }

        float dodgeRoll = Random.Range(0f, 100f);
        if (dodgeRoll < dodgeChance)
        {
            Debug.Log("DODGE! No damage taken.");
            return;
        }

        switch (damageType.ToLower())
        {
            case "physical":
                finalDamage *= 1f - physicalArmor / 100f;
                Debug.Log($"Physical damage reduced by armor. Final damage: {finalDamage}");
                break;
            case "magical":
                finalDamage *= 1f - magicArmor / 100f;
                break;
            case "pure":
                break;
            default:
                Debug.LogWarning("Unknown damage type: " + damageType);
                break;
        }

        finalDamage *= 1f - damageResistance / 100f;

        finalDamage = Mathf.Floor(finalDamage * 10f) / 10f;
        finalDamage = Mathf.Max(0f, finalDamage);

        SetHP(currentHP - finalDamage);
    }

    // Adds experience and triggers level-up(s) if the threshold is reached, handling overflow correctly
    public void AddExperience(int amount)
    {
        experience += amount;
        while (experience >= experienceToNextLevel)
        {
            experience -= experienceToNextLevel;
            LevelUp();
        }
    }

    // Deducts the given mana cost if the player has enough MP, returning true on success
    public bool ConsumeMana(float amount)
    {
        if (currentMP >= amount)
        {
            SetMP(currentMP - amount);
            return true;
        }
        return false;
    }

    // Injects dependencies and ensures the AudioSource component is available
    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // Each frame: handles death, drives the regen timer, and recalculates derived stats
    private void Update()
    {
        if (currentHP <= 0f)
        {
            Debug.Log("Player is dead! Respawning...");
            RestoreHealth();

            if (audioSource != null && deathMusic != null)
            {
                audioSource.PlayOneShot(deathMusic);
            }

            if (floorManager != null && floorManager.IsInsideDungeon)
            {
                floorManager.ExitAndDeleteDungeon();
            }
            else
            {
                floorManager.RespawnPlayerInWorld();
            }
            return;
        }

        updateHM();
        regenTimer += Time.deltaTime;
        if (regenTimer >= 1f)
        {
            RegenTick();
            regenTimer = 0f;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LevelUp();
        }
    }

    // Fully restores HP and MP to their current maximums
    public void RestoreHealth()
    {
        SetHP(maxHP);
        SetMP(maxMP);
    }

    // Applies one second of HP and MP regeneration if the respective resource is below its maximum
    private void RegenTick()
    {
        if (currentHP < maxHP && hpRegen > 0f)
        {
            SetHP(currentHP + hpRegen);
        }

        if (currentMP < maxMP && mpRegen > 0f)
        {
            SetMP(currentMP + mpRegen);
        }
    }
}
