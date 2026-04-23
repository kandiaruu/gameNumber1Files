using UnityEngine;

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
    [SerializeField] private ChainGenerator dungeonGenerator;
    [SerializeField] private float interactDistance = 10f;

    [InjectAttribute1] private IUIManager uiManager { get; set; }
    [InjectAttribute1] private IChestUIController chestUIController { get; set; }

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
            damagePopupSpawner.ShowDamage(hitEnemy.transform, dmg, false, DamageType.Magical);
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

    // 1) Вход в данж (обычный мир -> teleport room)
    // var entrance = hit.collider.GetComponentInParent<DungeonEntrance>();
    // if (entrance != null)
    // {
    //     Debug.Log("[Player] Interact -> DungeonEntrance");
    //     if (dungeonGenerator != null)
    //         dungeonGenerator.EnterDungeonFromWorld(transform);
    //     else
    //         Debug.LogWarning("[Player] dungeonGenerator is NULL");
    //     return;
    // }

    // 2) Двери внутри данжа (door-to-door)
    var interactable = hit.collider.GetComponentInParent<DoorInteractable>();
    if (interactable != null && interactable.portal != null)
    {
        dungeonGenerator.Interact(interactable.portal, hit.collider);
    }

    // 3) Сунд��к
    var chest = hit.collider.GetComponentInParent<Chest>();
    if (chest != null)
    {
        Debug.Log("[Player] Interact -> Chest");
        uiManager.OpenPanel(UIManager.PanelType.Inventory);
        chestUIController.OpenChestUI(chest);
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