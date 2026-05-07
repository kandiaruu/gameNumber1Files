using UnityEngine;

public class PlayerStats : MonoBehaviour, IPlayerStats
{
    [SerializeField] private int level = 1;
    [SerializeField] private int experience = 0;
    [SerializeField] private int experienceToNextLevel = 100;
    [SerializeField] private int gold = 0;
    [SerializeField] private int skillPoints = 0;
    [SerializeField] private int attributePoints = 0;

    [SerializeField] private int strength = 5; // урон, 
    [SerializeField] private int agility = 5; // скорость атаки, замедление времени + возможно уклонение и его интересноя механика
    [SerializeField] private int endurance = 5;  // сопротивление к усталости
    [SerializeField] private int perception = 0; // обояние, шанс крита озночает с какой вероятностью слабое место противника будет обнаружено, и если вы по этом месту попадете, то будет критический урон
    [SerializeField] private int intelligence = 0; // мана, воостановление маны
    [SerializeField] private int resistance = 0; // здоровье, воостановление здоровья
    [SerializeField] private int luck = 0;

    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float baseHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float maxMP = 50f;
    [SerializeField] private float baseMP = 50f;    
    [SerializeField] private float currentMP = 50f;
    [SerializeField] private float fatigue = 0f;

    [Header("Combat Stats")]
    [SerializeField] private float criticalChance = 0f;         // in %
    [SerializeField] private float criticalDamage = 25f;         // in %, e.g. 25 means +25% damage
    [SerializeField] private float critCooldown = 5f; // КД на появление новой крит точки
    [SerializeField] private float physicalArmor = 0f;           // in %
    [SerializeField] private float magicArmor = 0f;              // in %
    [SerializeField] private float dodgeChance = 0f;             // in %
    [SerializeField] private float damageResistance = 0f;        // in %, works against all except absolute
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deathMusic;

    [Header("Regen Stats")]
    [SerializeField] private float hpRegen = 1f;                // HP regen per second
    [SerializeField] private float mpRegen = 1f;                // MP regen per second
    [Header("Other")]
    // [SerializeField] private float damage = 0f;
    [SerializeField] private float atkSpeed = 2f;
    [SerializeField] private float atkRange = 2f; // Дистанция атаки по умолчанию
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

    public void LevelUp()
    {
        // Увеличиваем уровень
        level++;

        // Увеличиваем максимальные HP и MP на 100
        attributePoints += 1;
        skillPoints += 1;

        // Увеличиваем базовые атрибуты на 1
        strength += 1;
        agility += 1;
        endurance += 1;

        baseHP += 100f; // Обновляем базовые значения
        baseMP += 50f; // Обновляем базовые значения

        // Восстанавливаем HP и MP до нового максимума

        // Даем 1 очко атрибута и 1 скилл-поинт

        // Формула для увеличения опыта до следующего уровня (пример: экспоненциальный рост)
        // Можно менять формулу по желанию, вот пример:
        // experienceToNextLevel = (int)(experienceToNextLevel * 1.2f + 50 * level);
        experienceToNextLevel = (int)(100 * Mathf.Pow(1.1f, level - 1));
    }

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

    public void ResetAttributes()
    {
        int baseValue = 5 + (level - 1);

        int refundedPoints = 0;

        // Возвращаем вложенные ОЧКИ в силу, ловкость, выносливость
        refundedPoints += (strength - baseValue);
        refundedPoints += (agility - baseValue);
        refundedPoints += (endurance - baseValue);

        strength = baseValue;
        agility = baseValue;
        endurance = baseValue;

        // Возвращаем вложенные ОЧКИ в остальные (всё что было вложено сверх 0)
        refundedPoints += intelligence;
        refundedPoints += perception;
        refundedPoints += resistance;
        refundedPoints += luck;

        intelligence = 0;
        perception = 0;
        resistance = 0;
        luck = 0;

        attributePoints += refundedPoints;

        // (по желанию) обновить HP/MP, если ваши формулы зависят от этих параметров
        updateHM();
    }

    // Утилита для округления до 1 знака после запятой
    private float Round1(float value)
    {
        return Mathf.Floor(value * 10f) / 10f;
    }

    // Метод для установки HP/MP с округлением
    private void SetHP(float value)
    {
        currentHP = Mathf.Clamp(Round1(value), 0f, maxHP);
    }
    private void SetMP(float value)
    {
        currentMP = Mathf.Clamp(Round1(value), 0f, maxMP);
    }
    
    public float CalculateDamage(bool a) // не считает навыки, статусы, предметы, только базовый урон от сила
    {
        // Базовый урон от силы
        float baseDamage = strength * 2f;

        // Добавим бонус от уровня — экспоненциально, чтобы чувствовался рост
        baseDamage *= 1f + (level * 0.1f);

        // Учитываем шанс крита
        float finalDamage = baseDamage;
        // float critRoll = Random.Range(0f, 100f);

        // if (critRoll < criticalChance)
        // {
        //     finalDamage += baseDamage * (criticalDamage / 100f);
        //     Debug.Log($"CRITICAL HIT! Final damage: {finalDamage}");
        // }
        if (a == true)
        {
            finalDamage += baseDamage * (criticalDamage / 100f);
        }
        
        // Округлим
        finalDamage = Mathf.Floor(finalDamage * 10f) / 10f;
        return finalDamage;
    }

    // Метод получения урона с округлением урона до 1 знака (0.05 => 0.0, 0.12 => 0.1)
    public void TakeDamage(float damage, string damageType)
    {
        // Округление урона до 1 знака вниз (0.05 -> 0.0)

        float finalDamage = Mathf.Floor(damage * 10f) / 10f;
        Debug.Log($"Taking {finalDamage}");

        if (damageType.ToLower() == "absolute")
        {
            // Абсолютный урон: игнорирует всё, кроме крита
            SetHP(currentHP - finalDamage);
            return;
        }

        // Проверка уклонения
        float dodgeRoll = Random.Range(0f, 100f);
        if (dodgeRoll < dodgeChance)
        {
            Debug.Log("DODGE! No damage taken.");
            return;
        }

        // Применение брони/резистов
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
                // Pure damage игнорирует броню, но не сопротивление урону
                break;
            default:
                Debug.LogWarning("Unknown damage type: " + damageType);
                break;
        }

        // Сопротивление урону (кроме абсолютного)
        finalDamage *= 1f - damageResistance / 100f;

        // Округляем итоговый урон до 1 знака вниз
        finalDamage = Mathf.Floor(finalDamage * 10f) / 10f;
        finalDamage = Mathf.Max(0f, finalDamage);

        SetHP(currentHP - finalDamage);

    }
    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    // Восстановление HP и MP каждую секунду
    private void Update()
    {
        if (currentHP <= 0f)
        {
            Debug.Log("Player is dead! Respawning...");
            RestoreHealth(); // Полностью восстанавливаем HP и MP

            if (audioSource != null && deathMusic != null)
            {
                audioSource.PlayOneShot(deathMusic);
            }
            
            if (floorManager != null && floorManager.IsInsideDungeon)
            {
                // Удаляем подземелье и возвращаемся в мир
                floorManager.ExitAndDeleteDungeon(); 
            }
            else
            {
                floorManager.RespawnPlayerInWorld();
            }
            return; // Пропускаем реген и прочее в этом кадре
        }

        updateHM();
        regenTimer += Time.deltaTime;
        if (regenTimer >= 1f)
        {
            RegenTick();
            regenTimer = 0f;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TakeDamage(CalculateDamage(false),"Absolute");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LevelUp();
        }
    }

    public void RestoreHealth()
    {
        SetHP(maxHP);
        SetMP(maxMP);
    }

    // Метод восстановления HP и MP
    private void RegenTick()
    {
        if (currentHP < maxHP && hpRegen > 0f)
        {
            SetHP(currentHP + hpRegen);
            // Можно добавить Debug.Log($"HP реген: {hpRegen}. Текущее HP: {CurrentHP}");
        }

        if (currentMP < maxMP && mpRegen > 0f)
        {
            SetMP(currentMP + mpRegen);
            // Можно добавить Debug.Log($"MP реген: {mpRegen}. Текущее MP: {CurrentMP}");
        }
    }
    
    // Можно добавить методы прокачки, начисления опыта, ивенты и т.д.
}