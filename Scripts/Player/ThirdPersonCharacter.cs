using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class ThirdPersonCharacter : MonoBehaviour, IThirdPersonCharacter
{
    [InjectAttribute1] public IPlayerStats playerStats { get; set; }
    [SerializeField] private GameObject eKeyIcon;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    private float attackCooldown = 0f;
    public ThirdPersonCamera cameraController;
    private Rigidbody rb;
    private bool isGrounded;

    private static readonly Vector3 GroundCheckOffset = Vector3.up * 0.1f;
    [SerializeField] private Animator animator; // добавили
    [SerializeField] private DamagePopupSpawner damagePopupSpawner;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swordSwingSound;
    [SerializeField] private AudioClip swordHitSound;
    [SerializeField] private AudioClip castSound; 
    [Header("Магия")]
    [SerializeField] private GameObject fireballPrefab;     // Префаб файрбола
    [SerializeField] private Transform firePoint;   

    [SerializeField] private ChainGenerator dungeonGenerator;
    [SerializeField] private float interactDistance = 10f;
    private Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();

    [InjectAttribute1] private IUIManager uiManager { get; set; }
    [InjectAttribute1] private IChestUIController chestUIController { get; set; }
    [InjectAttribute1] private IInventoryPanel3 inventoryPanel3 { get; set; } 
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; } // <--- ДОБАВЛЕНО
    [InjectAttribute1] private IGameplaySkillManager gameplayManager { get; set; }
    [InjectAttribute1] private ISkillEquipManager equipManager { get; set; }
    [InjectAttribute1] private ILootManager3 lootManager3 { get; set; } // <--- ДОБАВЛЕНО

    private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (!rb)
                Debug.LogError("No Rigidbody component found on " + name);

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    // можно создать или просто написать ошибку
                    Debug.LogWarning("AudioSource is NULL on " + name + ". " +
                                    "Добавь компонент AudioSource и привяжи его в инспекторе.");
                }
            }
            // 1. Если что-то уже привязано в Inspector — оставляем
        }
    private void Start()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    private void Update()
    {
        if (transform.position.y <= -100f)
        {
            playerStats.TakeDamage(99999f, "absolute"); // Мгновенная смерть при падении
        }

        attackCooldown -= Time.deltaTime;
        CheckForInteractableAndToggleIcon();
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract(); // единая точка взаимодействия
        }
        // Jump handling
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        if (Input.GetMouseButtonDown(0) && attackCooldown <= 0f)
        {
            PlayAttackAnimation();
            attackCooldown = 1f / playerStats.AtkSpeed;
        }
        
        if (equipManager != null && equipManager.ActiveSkillKeys != null)
        {
            for (int i = 0; i < equipManager.ActiveSkillKeys.Length; i++)
            {
                // Если нажали нужную кнопку
                if (Input.GetKeyDown(equipManager.ActiveSkillKeys[i]))
                {
                    // Смотрим, какой скилл лежит в этом слоте
                    Skill skillInSlot = equipManager.EquippedActives[i];
                    if (skillInSlot != null)
                    {
                        CastSkill(skillInSlot.skillName);
                    }
                }
            }
        }
    }

    // Метод выстрела
    private void CastSkill(string skillName)
    {
        Skill skill = skillTreeManager?.GetAllSkills()?.FirstOrDefault(s => s.skillName == skillName);
        if (skill == null || !skill.isUnlocked || skill.skillPrefab == null) return;

        // 1. Читаем скрипт с ПРЕФАБА (не создавая его на сцене)
        Fireball prefabScript = skill.skillPrefab.GetComponent<Fireball>();
        if (prefabScript == null || prefabScript.statsPerLevel == null || prefabScript.statsPerLevel.Length == 0) return;

        // Находим нужные статы для текущего уровня
        int levelIndex = Mathf.Min(Mathf.Max(0, skill.currentLevel - 1), prefabScript.statsPerLevel.Length - 1);
        ActiveSkillStats currentStats = prefabScript.statsPerLevel[levelIndex];

        // 2. Проверка кулдауна
        if (skillCooldowns.TryGetValue(skillName, out float cdEndTime))
        {
            if (Time.time < cdEndTime)
            {
                if (damagePopupSpawner != null) 
                    damagePopupSpawner.ShowMessage(transform, "Перезарядка...", Color.yellow);
                return;
            }
        }

        // 3. Проверка Маны (если у вас в IPlayerStats есть мана)
        /*
        if (playerStats.Mana < currentStats.manaCost)
        {
            if (damagePopupSpawner != null) 
                damagePopupSpawner.ShowMessage(transform, "Нет маны!", Color.blue);
            return;
        }
        playerStats.Mana -= currentStats.manaCost;
        */

        // 4. Считаем ИТОГОВЫЙ УРОН через наш новый менеджер
        float baseDamage = currentStats.damage;
        float finalDamage = gameplayManager != null ? 
            gameplayManager.CalculateFinalDamage(baseDamage, skill.tags) : baseDamage;

        // 5. Ставим кулдаун
        skillCooldowns[skillName] = Time.time + currentStats.cooldown;

        // 6. Выстрел
        if (skill.castSound != null && audioSource != null) audioSource.PlayOneShot(skill.castSound);

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 500f)) targetPoint = hit.point;
        else targetPoint = ray.GetPoint(500f);

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        GameObject spawnedProjectile = Instantiate(skill.skillPrefab, spawnPos, Quaternion.identity);
        spawnedProjectile.transform.LookAt(targetPoint);

        // 7. Передаем готовые данные в созданный снаряд
        Fireball spawnedScript = spawnedProjectile.GetComponent<Fireball>();
        if (spawnedScript != null)
        {
            spawnedScript.currentSkillLevel = skill.currentLevel;
            spawnedScript.calculatedDamage = finalDamage; // <--- Передаем уже финальную цифру!
            spawnedScript.skillTags = skill.tags;
            spawnedScript.gameplayManager = gameplayManager;
        }
    }

    public void OnSwing()
    {
        // звук
        if (audioSource != null && swordSwingSound != null)
        {
            audioSource.PlayOneShot(swordSwingSound);
        }
    }
    public void OnAttackHit()
    {
        TryAttackEnemy();
    }

    private void PlayAttackAnimation()
    {
        //if (animator == null) return;

        // Скорость анимации зависит от AtkSpeed
        Debug.Log("PlayAttackAnimation called");
        float baseAttackSpeed = 1f; // клип делали под AtkSpeed = 1
        float animSpeed = Mathf.Max(0.1f, playerStats.AtkSpeed / baseAttackSpeed);
        animator.speed = animSpeed;

        animator.SetTrigger("Attack");
    }

    private void TryAttackEnemy()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Camera cam = cameraController.GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(screenCenter);

        float attackRange = playerStats.AtkRange;

        RaycastHit[] hits = Physics.RaycastAll(ray, attackRange);

        EnemyAI hitEnemy = null;
        bool critHit = false;

        foreach (var hit in hits)
        {
            // Сначала ищем CritPoint
            if (hit.collider.CompareTag("CritPoint"))
            {
                var critMarker = hit.collider.GetComponent<CritPointMarker>();
                if (critMarker != null)
                {
                    if (audioSource != null && swordHitSound != null)
                    {
                        audioSource.PlayOneShot(swordHitSound);
                    }
                    critMarker.OnCritHit();
                    float dmg = playerStats.CalculateDamage(true);
                    critMarker.owner.TakeDamage(dmg);
                    damagePopupSpawner.ShowDamage(critMarker.owner.transform, dmg, true, DamageType.Pure);
                    Debug.Log("Critical Hit!");
                    critHit = true;
                    break; // Сразу выходим — крит приоритетнее обычного
                }
            }
            // Запоминаем обычного врага, если не было крита
            else if (hit.collider.CompareTag("Enemy") && !critHit)
            {
                hitEnemy = hit.collider.GetComponent<EnemyAI>();
            }
        }

        // Если не было крита, но попали по врагу — обычный урон
        if (!critHit && hitEnemy != null)
        {
            if (audioSource != null && swordHitSound != null)
            {
                audioSource.PlayOneShot(swordHitSound);
            }
            float dmg = playerStats.CalculateDamage(false);
            hitEnemy.TakeDamage(dmg);
            damagePopupSpawner.ShowDamage(hitEnemy.transform, dmg, false, DamageType.Physical);
            Debug.Log("Normal Hit!");
        }
    }

private void CheckForInteractableAndToggleIcon()
{
    Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
    Ray ray = cameraController.GetComponent<Camera>().ScreenPointToRay(screenCenter);

    bool lookingAtInteractable = false;

    if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
    {
        // Вход в данж из обычного мира
        if (hit.collider.GetComponentInParent<DungeonEntrance>() != null)
            lookingAtInteractable = true;
        // Двери данжа
        else if (hit.collider.GetComponentInParent<DoorInteractable>() != null)
            lookingAtInteractable = true;
        // Сундуки
        else if (hit.collider.GetComponentInParent<Chest>() != null)
            lookingAtInteractable = true;
        else if (hit.collider.GetComponentInParent<BossRoomPortal>() != null)
            lookingAtInteractable = true;
        else if (hit.collider.GetComponentInParent<BackBossRoomPortal>() != null)
            lookingAtInteractable = true;
        else if (hit.collider.GetComponentInParent<MerchantNPC>() != null)
            lookingAtInteractable = true;
    }

    if (eKeyIcon != null)
        eKeyIcon.SetActive(lookingAtInteractable);
}

private void TryInteract()
{
    Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
    Ray ray = cameraController.GetComponent<Camera>().ScreenPointToRay(screenCenter);

    if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance))
    {
        Debug.Log("[Player] TryInteract: raycast missed");
        return;
    }

    Debug.Log("[Player] Hit: " + hit.collider.name);

    var merchant = hit.collider.GetComponentInParent<MerchantNPC>();
    if (merchant != null)
    {
        if (uiManager != null)
        {
            // Открываем панель торговца (убедитесь, что название совпадает с вашим enum в UIManager)
            uiManager.OpenPanel(UIManager.PanelType.MerchantPanel);
        }
        return; 
    }

    var interactable = hit.collider.GetComponentInParent<DoorInteractable>();
    if (interactable != null && interactable.portal != null)
    {
        if (interactable.portal.owner != null)
        {
            var enemiesInRoom = interactable.portal.owner.GetComponentsInChildren<EnemyAI>();
            foreach (var enemy in enemiesInRoom)
            {
                if (enemy.IsAlive)
                {
                    // Вызываем всплывающий текст прямо над дверью (красным цветом)
                    if (damagePopupSpawner != null)
                    {
                        damagePopupSpawner.ShowMessage(interactable.transform, "Убейте всех врагов!", Color.red);
                    }
                    return; 
                }
            }
        }

        dungeonGenerator.Interact(interactable.portal, hit.collider);
        return;
    }

    var chest = hit.collider.GetComponentInParent<Chest>();
    if (chest != null)
    {
        if (chest.isLocked)
        {
            if (inventoryPanel3 != null && inventoryPanel3.HasItem(4, 1))
            {
                inventoryPanel3.ClearItem(4, 1); 
                chest.isLocked = false;    
                if (lootManager3 != null)
                {
                    lootManager3.AddChestOpen();
                    uiManager.OpenPanel(UIManager.PanelType.Inventory3);
                }      
            }
            else
            {
                if (damagePopupSpawner != null)
                    damagePopupSpawner.ShowMessage(chest.transform, "Нужна отмычка!", Color.red);
            }
            return;
        }

        if (damagePopupSpawner != null)
            damagePopupSpawner.ShowMessage(chest.transform, "В сундуке ничего нет!", Color.red);
        return;
    }

    // 4 Teleporter
        // 4 Teleporter
    var bossPortal = hit.collider.GetComponentInParent<BossRoomPortal>();
    if (bossPortal != null)
    {
        // <--- ДОБАВЛЕНО: Проверка жив ли босс
        var currentRoom = bossPortal.GetComponentInParent<NodeInstance>();
        if (currentRoom != null)
        {
            var enemiesInRoom = currentRoom.GetComponentsInChildren<EnemyAI>();
            foreach (var enemy in enemiesInRoom)
            {
                if (enemy.IsAlive)
                {
                    // Босс всё ещё жив! Показываем сообщение и прерываем телепортацию
                    if (damagePopupSpawner != null)
                    {
                        damagePopupSpawner.ShowMessage(bossPortal.transform, "Сначала победите босса!", Color.red);
                    }
                    return; 
                }
            }
        }
        // --->

        // Если дошли сюда — босс мёртв (или его не было), телепортируемся!
        var floorManager = FindFirstObjectByType<DungeonFloorManager>();
        if (floorManager != null)
        {
            floorManager.StartNextFloor();
        }
        return;
    }
    
    var backBossPortal = hit.collider.GetComponentInParent<BackBossRoomPortal>();
    if (backBossPortal != null)
    {
        var floorManager = FindFirstObjectByType<DungeonFloorManager>();
        if (floorManager != null)
        {
            floorManager.GoToPreviousFloor();
        }
        return;
    }
    
    var entryPortal = hit.collider.GetComponent<DungeonEntrance>();
    if (entryPortal != null)
    {
        var floorManager = FindFirstObjectByType<DungeonFloorManager>();
        if (floorManager != null)
        {
            // Вызываем умный метод переключения[cite: 4]
            uiManager.OpenPanel(UIManager.PanelType.DungeonEntry);
        }
        return;
    }
}

    private void FixedUpdate()
    {
        // Ground check
        isGrounded = Physics.Raycast(transform.position + GroundCheckOffset, Vector3.down, groundCheckDistance + 0.1f);

        // Handle movement relative to camera yaw
        Vector3 inputDirection = GetInputDirection();
        Move(inputDirection);
        RotateWithCamera();
    }

    private Vector3 GetInputDirection()
    {
        return new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical")
        ).normalized;
    }

    private void Move(Vector3 inputDirection)
    {
        float deltaTime = Time.fixedDeltaTime;
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift);

        float speed = wantsToSprint ? sprintSpeed : moveSpeed;

        float yaw = cameraController != null ? cameraController.transform.eulerAngles.y : transform.eulerAngles.y;
        Vector3 moveDirection = Quaternion.Euler(0, yaw, 0) * inputDirection;

        Vector3 velocity = moveDirection * speed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }

    private void RotateWithCamera()
    {
        if (cameraController == null) return;
        // Always face the direction the camera is looking (Y axis only)
        Vector3 euler = transform.eulerAngles;
        euler.y = cameraController.transform.eulerAngles.y;
        transform.eulerAngles = euler;
    }

    private void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }
}