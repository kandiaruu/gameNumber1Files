//
// ThirdPersonCharacter controls the player in a third-person Unity game. It handles
// movement, sprinting, jumping, melee attacks, skill casting (e.g. fireballs),
// environment interaction (chests, doors, portals, merchants), ground detection,
// camera-aligned rotation, and animator/audio integration.
//

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
    [SerializeField] private Animator animator;
    [SerializeField] private DamagePopupSpawner damagePopupSpawner;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swordSwingSound;
    [SerializeField] private AudioClip swordHitSound;
    [SerializeField] private AudioClip castSound;
    [Header("Magic")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform firePoint;

    [SerializeField] private ChainGenerator dungeonGenerator;
    [SerializeField] private float interactDistance = 10f;
    private Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();

    [InjectAttribute1] private IUIManager uiManager { get; set; }
    [InjectAttribute1] private IInventoryPanel3 inventoryPanel3 { get; set; }
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; }
    [InjectAttribute1] private IGameplaySkillManager gameplayManager { get; set; }
    [InjectAttribute1] private ISkillEquipManager equipManager { get; set; }
    [InjectAttribute1] private ILootManager3 lootManager3 { get; set; }

    // Fetches required components (Rigidbody, AudioSource) and logs warnings if any are missing
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
                Debug.LogWarning("AudioSource is NULL on " + name + ". " +
                                "Add an AudioSource component and assign it in the Inspector.");
            }
        }
    }

    // Injects all interface dependencies via the dependency container
    private void Start()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    // Called every frame: handles fall damage, attack cooldown, interaction prompt, jumping, melee input, and active skill key input
    private void Update()
    {
        if (transform.position.y <= -100f)
        {
            playerStats.TakeDamage(99999f, "absolute");
        }

        attackCooldown -= Time.deltaTime;
        CheckForInteractableAndToggleIcon();
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }

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
                if (Input.GetKeyDown(equipManager.ActiveSkillKeys[i]))
                {
                    Skill skillInSlot = equipManager.EquippedActives[i];
                    if (skillInSlot != null)
                    {
                        CastSkill(skillInSlot.skillName);
                    }
                }
            }
        }
    }

    // Validates, calculates damage for, and fires a projectile skill by name, respecting cooldowns and skill level stats
    private void CastSkill(string skillName)
    {
        Skill skill = skillTreeManager?.GetAllSkills()?.FirstOrDefault(s => s.skillName == skillName);
        if (skill == null || !skill.isUnlocked || skill.skillPrefab == null) return;

        Fireball prefabScript = skill.skillPrefab.GetComponent<Fireball>();
        if (prefabScript == null || prefabScript.statsPerLevel == null || prefabScript.statsPerLevel.Length == 0) return;

        int levelIndex = Mathf.Min(Mathf.Max(0, skill.currentLevel - 1), prefabScript.statsPerLevel.Length - 1);
        ActiveSkillStats currentStats = prefabScript.statsPerLevel[levelIndex];

        if (skillCooldowns.TryGetValue(skillName, out float cdEndTime))
        {
            if (Time.time < cdEndTime)
            {
                if (damagePopupSpawner != null)
                    damagePopupSpawner.ShowMessage("Reloading...", Color.yellow);
                return;
            }
        }

        if (!playerStats.ConsumeMana(currentStats.manaCost))
        {
            if (damagePopupSpawner != null)
                damagePopupSpawner.ShowMessage("Not enough mana!", Color.blue);
            return;
        }

        float baseDamage = currentStats.damage;
        float finalDamage = gameplayManager != null ?
            gameplayManager.CalculateFinalDamage(baseDamage, skill.tags) : baseDamage;

        skillCooldowns[skillName] = Time.time + currentStats.cooldown;

        if (skill.castSound != null && audioSource != null) audioSource.PlayOneShot(skill.castSound);

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 500f)) targetPoint = hit.point;
        else targetPoint = ray.GetPoint(500f);

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        GameObject spawnedProjectile = Instantiate(skill.skillPrefab, spawnPos, Quaternion.identity);
        spawnedProjectile.transform.LookAt(targetPoint);

        Fireball spawnedScript = spawnedProjectile.GetComponent<Fireball>();
        if (spawnedScript != null)
        {
            spawnedScript.currentSkillLevel = skill.currentLevel;
            spawnedScript.calculatedDamage = finalDamage;
            spawnedScript.skillTags = skill.tags;
            spawnedScript.gameplayManager = gameplayManager;
        }
    }

    // Animation event callback: plays the sword swing sound when the swing frame is reached
    public void OnSwing()
    {
        if (audioSource != null && swordSwingSound != null)
        {
            audioSource.PlayOneShot(swordSwingSound);
        }
    }

    // Animation event callback: triggers the actual hit detection when the attack animation connects
    public void OnAttackHit()
    {
        TryAttackEnemy();
    }

    // Sets the animator speed to match the player's attack speed stat and triggers the Attack animation
    private void PlayAttackAnimation()
    {
        Debug.Log("PlayAttackAnimation called");
        float baseAttackSpeed = 1f;
        float animSpeed = Mathf.Max(0.1f, playerStats.AtkSpeed / baseAttackSpeed);
        animator.speed = animSpeed;

        animator.SetTrigger("Attack");
    }

    // Raycasts from the screen centre to detect enemies; applies critical or normal damage depending on what was hit
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
                    break;
                }
            }
            else if (hit.collider.CompareTag("Enemy") && !critHit)
            {
                hitEnemy = hit.collider.GetComponent<EnemyAI>();
            }
        }

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

    // Raycasts from the screen centre and shows or hides the E-key interaction icon based on whether an interactable object is in range
    private void CheckForInteractableAndToggleIcon()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = cameraController.GetComponent<Camera>().ScreenPointToRay(screenCenter);

        bool lookingAtInteractable = false;

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            if (hit.collider.GetComponentInParent<DungeonEntrance>() != null)
                lookingAtInteractable = true;
            else if (hit.collider.GetComponentInParent<DoorInteractable>() != null)
                lookingAtInteractable = true;
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

    // Raycasts from the screen centre and delegates to the appropriate handler for the object hit (merchant, door, chest, portal, dungeon entrance)
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
                        if (damagePopupSpawner != null)
                        {
                            damagePopupSpawner.ShowMessage("Kill all enemies!", Color.red);
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
                        damagePopupSpawner.ShowMessage("You need a lockpick!", Color.red);
                }
                return;
            }

            if (damagePopupSpawner != null)
                damagePopupSpawner.ShowMessage("The chest is empty!", Color.red);
            return;
        }

        var bossPortal = hit.collider.GetComponentInParent<BossRoomPortal>();
        if (bossPortal != null)
        {
            var currentRoom = bossPortal.GetComponentInParent<NodeInstance>();
            if (currentRoom != null)
            {
                var enemiesInRoom = currentRoom.GetComponentsInChildren<EnemyAI>();
                foreach (var enemy in enemiesInRoom)
                {
                    if (enemy.IsAlive)
                    {
                        if (damagePopupSpawner != null)
                        {
                            damagePopupSpawner.ShowMessage("Defeat the boss first!", Color.red);
                        }
                        return;
                    }
                }
            }

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
                uiManager.OpenPanel(UIManager.PanelType.DungeonEntry);
            }
            return;
        }
    }

    // Called every physics step: checks if the player is grounded, reads input, moves, and rotates to match the camera
    private void FixedUpdate()
    {
        isGrounded = Physics.Raycast(transform.position + GroundCheckOffset, Vector3.down, groundCheckDistance + 0.1f);

        Vector3 inputDirection = GetInputDirection();
        Move(inputDirection);
        RotateWithCamera();
    }

    // Returns the normalised horizontal input direction from the WASD / arrow keys
    private Vector3 GetInputDirection()
    {
        return new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical")
        ).normalized;
    }

    // Moves the rigidbody in the camera-relative direction at walk or sprint speed, preserving vertical velocity
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

    // Rotates the player on the Y axis to always face the same direction as the camera
    private void RotateWithCamera()
    {
        if (cameraController == null) return;
        Vector3 euler = transform.eulerAngles;
        euler.y = cameraController.transform.eulerAngles.y;
        transform.eulerAngles = euler;
    }

    // Applies an upward impulse force to make the player jump and marks the player as airborne
    private void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }
}
