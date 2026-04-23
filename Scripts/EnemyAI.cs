using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private float originalSpeed = 4f; // Изначальная скорость движения
    private float originalAttackRushSpeedMultiplier = 6f; // Изначальный множитель скорости атаки в rush
    private float originalAtkSpeed = 0.5f; // Изначальная скорость атаки
    public GameObject chestPrefab;
    public int strength = 5;
    public int agility = 5;
    public float speed = 3f;
    public float detectionRadius = 10f;
    public float minDistanceToPlayer = 1.5f;
    public float minAttackDistance = 3f; // Новая переменная: дистанция для начала rush
    public float maxHP = 30f;
    private float currentHP;

    public float damage = 10f;
    public float atkSpeed = 0.5f;
    public GameObject critPointPrefab;
    public Transform[] critPointSpots;

    [InjectAttribute1] public IPlayerStats playerStats { get; set; }
    private float attackCooldown = 0f;

    // --- Для rush ("замаха") ---
    public float attackRushSpeedMultiplier = 6f;
    public float attackRushCooldown = 5f;
    private bool isRushing = false;
    private float rushCooldownTimer = 0f;
    private float rushDuration = 0.5f; // длительность rush
    private float rushTimer = 0f;
    private bool hasDealtRushDamage = false;

    private Transform player;

    // --- Критическая логика ---
    private GameObject currentCritPoint;
    private float critTimer = 0f;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;   // можно назначить в инспекторе
    [SerializeField] private AudioClip rushAttackClip;  // сюда перетащи mp3

    void Awake()
    {
        currentHP = maxHP;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void TakeDamage(float amount)
    {
        // Получаем силу игрока (через DI или другим способом)
        int playerStrength = playerStats.Strength;

        // Сравниваем силу игрока и врага
        if (playerStrength >= strength * 3)
        {
            // Сила игрока больше в 10 раз — мгновенная смерть врага
            Debug.Log($"[EnemyAI] Player's strength ({playerStrength}) is too high, enemy dies instantly.");
            Die();
            return;
        }
        else if (playerStrength >= strength * 2)
        {
            // В 2 раза больше — в 2 раза больше урона
            Debug.Log($"2");
            amount *= 2f;
        }
        else if (playerStrength < strength)
        {
            Debug.Log($"0.5");
            // Слабее — в 2 раза меньше урона
            amount *= 0.5f;
        }
        // если силы равны — обычный урон

        currentHP -= amount;
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // if (chestPrefab != null)
        // {
        //     Instantiate(chestPrefab, transform.position, Quaternion.identity);
        // }
        Destroy(gameObject);
    }

    void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        critTimer = 0f; // сразу пытаемся заспавнить крит-точку
    }

    void Update()
    {
        int playerAgility = playerStats.Agility;

        if (playerAgility >= agility * 3)
        {
            // Игрок быстрее в 3 раза — враг абсолютно парализован
            speed = 0f;
            attackRushSpeedMultiplier = 0f;
            atkSpeed = 0f;
            // Можно добавить return, чтобы враг вообще ничего не делал
            return;
        }
        else if (playerAgility >= agility * 2)
        {
            // Игрок быстрее в 2 раза — враг в 2 раза медленнее
            speed = originalSpeed * 0.5f;
            attackRushSpeedMultiplier = originalAttackRushSpeedMultiplier * 0.5f;
            atkSpeed = originalAtkSpeed * 0.5f;
        }
        else
        {
            // Восстанавливаем исходные значения, если ловкость снова сравнялась
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
            // --- Обычное движение если вне зоны rush ---
                if (dist < detectionRadius && dist > minAttackDistance)
                {
                    MoveTowardsPlayer(speed);
                }

            // --- Входим в зону rush ---
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

                    // Проверяем, не настало ли время остановить rush
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

            // --- Если rush не активен и входим в minDistanceToPlayer — обычная атака ---
            // if (!isRushing && dist <= minDistanceToPlayer)
            // {
            //     AttackPlayer();
            // }

            // Поворачиваемся к игроку если двигаемся
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

        // --- Критическая точка логика ---
        UpdateCritPointLogic();
    }

    void MoveTowardsPlayer(float currentSpeed)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    void StartRush()
    {
        isRushing = true;
        hasDealtRushDamage = false; // сбрасываем флаг на начало rush
        rushTimer = rushDuration;
        rushCooldownTimer = attackRushCooldown;
        // TODO: тут можно запустить анимацию rush
        // animator.SetTrigger("AttackRush");
    }

    void StopRush()
    {
        isRushing = false;
        rushTimer = 0f;
        // TODO: тут можно выключить анимацию rush
    }

    void RushAttackPlayer()
    {
        playerStats.TakeDamage(20f, "absolute"); // Особый урон
        hasDealtRushDamage = true; // чтобы не нанести повторно в одном rush
        // TODO: можно триггерить особую анимацию попадания/удара
        if (rushAttackClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(rushAttackClip);
        }
    }

    void AttackPlayer()
    {
        attackCooldown -= Time.deltaTime;
        if (attackCooldown <= 0f)
        {
            playerStats.TakeDamage(damage, "absolute");
            attackCooldown = 1f / atkSpeed;
        }
    }

    void UpdateCritPointLogic()
    {
        // Если крит-точка уже есть — ничего не делаем
        if (currentCritPoint != null) return;

        // Ждём кулдауна
        critTimer -= Time.deltaTime;
        if (critTimer > 0f) return;

        // Пытаемся заспавнить критическую точку
        bool spawned = TrySpawnCritPoint();
        if (!spawned)
        {
            // Не получилось — снова ждём cooldown
            critTimer = playerStats.CritCooldown;
        }
    }

    // Вызывается при попадании по крит-точке!
    public void OnCritPointDestroyed()
    {
        currentCritPoint = null;
        critTimer = playerStats.CritCooldown;
    }

    // Пытаемся заспавнить одну крит-точку
    bool TrySpawnCritPoint()
    {
        if (critPointSpots == null || critPointSpots.Length == 0) return false;

        float critChance = playerStats.CriticalChance / 100f;

        // Сначала решаем, будет ли точка вообще
        if (Random.value < critChance)
        {
            // Если да — выбираем случайную позицию
            int idx = Random.Range(0, critPointSpots.Length);
            Transform spot = critPointSpots[idx];
            currentCritPoint = Instantiate(critPointPrefab, spot.position, spot.rotation, spot);
            var marker = currentCritPoint.GetComponent<CritPointMarker>();
            if (marker != null)
                marker.owner = this;
            // Debug.Log($"[EnemyAI] CritPoint spawn attempt : SUCCESS (at spot {idx})");
            return true;
        }

        // Debug.Log($"[EnemyAI] CritPoint spawn attempt : FAIL (no crit point this time)");
        return false; // точка не появилась
    }
}