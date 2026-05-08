//
// EnemyAI controls enemy behaviour in the dungeon. It handles movement toward the player,
// a rush attack with cooldown, standard melee attacks, damage scaling based on the
// player's strength and agility stats, death with loot registration, and a crit-point
// system that spawns a weak-spot on the enemy for the player to target.
//

using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private float originalSpeed = 4f;
    private float originalAttackRushSpeedMultiplier = 6f;
    private float originalAtkSpeed = 0.5f;
    public GameObject chestPrefab;
    public int strength = 5;
    public int agility = 5;
    public float speed = 3f;
    public float detectionRadius = 10f;
    public float minDistanceToPlayer = 1.5f;
    public float minAttackDistance = 3f;
    public float maxHP = 30f;
    private float currentHP;

    public float damage = 10f;
    public float atkSpeed = 0.5f;
    public GameObject critPointPrefab;
    public Transform[] critPointSpots;

    [InjectAttribute1] public IPlayerStats playerStats { get; set; }
    [InjectAttribute1] public ILootManager3 lootManager3 { get; set; }
    private float attackCooldown = 0f;

    public float attackRushSpeedMultiplier = 6f;
    public float attackRushCooldown = 5f;
    private bool isRushing = false;
    private float rushCooldownTimer = 0f;
    private float rushDuration = 0.5f;
    private float rushTimer = 0f;
    private bool hasDealtRushDamage = false;

    private Transform player;

    private GameObject currentCritPoint;
    private float critTimer = 0f;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip rushAttackClip;

    // Initialises HP and ensures an AudioSource component is present
    void Awake()
    {
        currentHP = maxHP;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // Applies incoming damage scaled by the player's strength relative to the enemy's, then calls Die if HP reaches zero
    public void TakeDamage(float amount)
    {
        int playerStrength = playerStats.Strength;

        if (playerStrength >= strength * 3)
        {
            Debug.Log($"[EnemyAI] Player's strength ({playerStrength}) is too high, enemy dies instantly.");
            Die();
            return;
        }
        else if (playerStrength >= strength * 2)
        {
            Debug.Log($"2");
            amount *= 2f;
        }
        else if (playerStrength < strength)
        {
            Debug.Log($"0.5");
            amount *= 0.5f;
        }

        currentHP -= amount;
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    // Registers a goblin kill, gives XP, shows popup and destroys this GameObject
    private void Die()
    {
        if (lootManager3 != null)
        {
            lootManager3.AddGoblinKill();
        }

        if (playerStats != null)
        {
            playerStats.AddExperience(3);
        }

        DamagePopupSpawner spawner = FindFirstObjectByType<DamagePopupSpawner>();
        if (spawner != null)
        {
            spawner.ShowMessage("+3 XP", Color.green);
        }

        Destroy(gameObject);
    }

    // Returns true if the enemy still has HP remaining
    public bool IsAlive => currentHP > 0f;

    // Injects dependencies, locates the player by tag, and resets the crit-point timer
    void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        critTimer = 0f;
    }

    // Called every frame: adjusts speed based on player agility, manages rush cooldown and movement, rotates toward the player, and updates the crit-point
    void Update()
    {
        int playerAgility = playerStats.Agility;

        if (playerAgility >= agility * 3)
        {
            speed = 0f;
            attackRushSpeedMultiplier = 0f;
            atkSpeed = 0f;
            return;
        }
        else if (playerAgility >= agility * 2)
        {
            speed = originalSpeed * 0.5f;
            attackRushSpeedMultiplier = originalAttackRushSpeedMultiplier * 0.5f;
            atkSpeed = originalAtkSpeed * 0.5f;
        }
        else
        {
            speed = originalSpeed;
            attackRushSpeedMultiplier = originalAttackRushSpeedMultiplier;
            atkSpeed = originalAtkSpeed;
        }

        if (rushCooldownTimer > 0f)
            rushCooldownTimer -= Time.deltaTime;

        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= minDistanceToPlayer && isRushing && !hasDealtRushDamage)
            {
                RushAttackPlayer();
            }
            if (isRushing)
            {
                Debug.Log("[EnemyAI] Currently rushing, cannot move normally.");
            }

            if (dist < detectionRadius && dist > minAttackDistance)
            {
                MoveTowardsPlayer(speed);
            }

            if (dist <= minAttackDistance && dist > minDistanceToPlayer)
            {
                if (rushCooldownTimer <= 0f && !isRushing)
                {
                    StartRush();
                }
                if (isRushing)
                {
                    rushTimer -= Time.deltaTime;
                    MoveTowardsPlayer(speed * attackRushSpeedMultiplier);

                    if (rushTimer <= 0f)
                    {
                        StopRush();
                    }
                }
            }
            else
            {
                if (isRushing)
                {
                    StopRush();
                }
            }

            if (dist < detectionRadius && dist > minDistanceToPlayer)
            {
                Vector3 lookDir = player.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
                }
            }
        }

        UpdateCritPointLogic();
    }

    // Moves the enemy directly toward the player at the given speed
    void MoveTowardsPlayer(float currentSpeed)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    // Begins a rush attack: sets the rushing flag, resets damage-dealt flag, and starts the rush and cooldown timers
    void StartRush()
    {
        isRushing = true;
        hasDealtRushDamage = false;
        rushTimer = rushDuration;
        rushCooldownTimer = attackRushCooldown;
    }

    // Ends the rush attack and resets the rush timer
    void StopRush()
    {
        isRushing = false;
        rushTimer = 0f;
    }

    // Deals a fixed burst of damage to the player on a successful rush contact, plays the rush sound, and flags that damage has been dealt this rush
    void RushAttackPlayer()
    {
        playerStats.TakeDamage(20f, "absolute");
        hasDealtRushDamage = true;
        if (rushAttackClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(rushAttackClip);
        }
    }

    // Deals damage to the player at a rate determined by atkSpeed, using an internal cooldown timer
    void AttackPlayer()
    {
        attackCooldown -= Time.deltaTime;
        if (attackCooldown <= 0f)
        {
            playerStats.TakeDamage(damage, "absolute");
            attackCooldown = 1f / atkSpeed;
        }
    }

    // Manages the crit-point lifecycle: does nothing if one exists, otherwise counts down and attempts to spawn a new one
    void UpdateCritPointLogic()
    {
        if (currentCritPoint != null) return;

        critTimer -= Time.deltaTime;
        if (critTimer > 0f) return;

        bool spawned = TrySpawnCritPoint();
        if (!spawned)
        {
            critTimer = playerStats.CritCooldown;
        }
    }

    // Called by the crit-point when it is destroyed; clears the reference and starts the respawn cooldown
    public void OnCritPointDestroyed()
    {
        currentCritPoint = null;
        critTimer = playerStats.CritCooldown;
    }

    // Rolls against the player's critical chance and, on success, instantiates a crit-point prefab at a random spot on the enemy
    bool TrySpawnCritPoint()
    {
        if (critPointSpots == null || critPointSpots.Length == 0) return false;

        float critChance = playerStats.CriticalChance / 100f;

        if (Random.value < critChance)
        {
            int idx = Random.Range(0, critPointSpots.Length);
            Transform spot = critPointSpots[idx];
            currentCritPoint = Instantiate(critPointPrefab, spot.position, spot.rotation, spot);
            var marker = currentCritPoint.GetComponent<CritPointMarker>();
            if (marker != null)
                marker.owner = this;
            return true;
        }

        return false;
    }
}
