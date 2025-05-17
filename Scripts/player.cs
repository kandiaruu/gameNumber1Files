using UnityEngine;

public class ThirdPersonCharacter : MonoBehaviour, IThirdPersonCharacter
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 1.67f;
    [SerializeField] private float staminaRegenDelay = 15f;
    [SerializeField] private float interactRange = 2f;
    public ThirdPersonCamera cameraController;
    private Rigidbody rb;
    private Vector3 moveVelocity;
    private bool isGrounded;
    private float currentStamina;
    private float timeSinceLastSprint;

    private static readonly Vector3 GroundCheckOffset = Vector3.up * 0.1f;
    private static readonly KeyCode[] MovementKeys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.W };
    [InjectAttribute1] private IUIManager uiManager { get; set; }
    [InjectAttribute1] private IChestUIController chestUIController { get; set; }

    private void Awake()
    {
        
        InitializeComponents();
        currentStamina = maxStamina;
        timeSinceLastSprint = staminaRegenDelay;
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) Debug.LogError("No Rigidbody component found!");

        cameraController ??= Object.FindFirstObjectByType<ThirdPersonCamera>();
        if (!cameraController) Debug.LogError("No ThirdPersonCamera found!");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryOpenNearbyChest();
        }
        // Stamina regeneration
        if (!Input.GetKey(KeyCode.LeftShift) || currentStamina <= 0)
        {
            timeSinceLastSprint += Time.deltaTime;
            if (timeSinceLastSprint >= staminaRegenDelay)
            {
                RegenerateStamina();
            }
        }

        // Jump handling
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    void TryOpenNearbyChest()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Chest chest))
            {
                uiManager.OpenPanel(UIManager.PanelType.Inventory);
                chestUIController.OpenChestUI(chest);
                break;
            }
        }
    }

    private void FixedUpdate()
    {
        // Ground check
        isGrounded = Physics.Raycast(transform.position + GroundCheckOffset, 
            Vector3.down, groundCheckDistance + 0.1f);

        // Handle movement and rotation
        Vector3 inputDirection = GetInputDirection();
        Move(inputDirection);
        Rotate(inputDirection);

        // If no input, dampen movement
        if (inputDirection.sqrMagnitude < 0.01f)
        {
            DampenMovement();
        }
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
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            bool wantsToSprint = Input.GetKey(KeyCode.LeftShift);
            float speed = (wantsToSprint && currentStamina > 0) ? sprintSpeed : moveSpeed;

            if (wantsToSprint && currentStamina > 0)
            {
                currentStamina = Mathf.Max(0, currentStamina - staminaDrainRate * deltaTime);
                timeSinceLastSprint = 0f;
            }

            Vector3 moveDirection = Quaternion.Euler(0, cameraController.yaw, 0) * inputDirection;
            moveVelocity = moveDirection * speed;
            rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
        }
    }

    private void Rotate(Vector3 inputDirection)
    {
        float deltaTime = Time.fixedDeltaTime;
        float cameraYaw = cameraController.yaw;
        Quaternion targetRotation;

        if (inputDirection.sqrMagnitude > 0.01f && IsAnyMovementKeyPressed())
        {
            Vector3 moveDirection = Quaternion.Euler(0, cameraYaw, 0) * inputDirection;
            targetRotation = Quaternion.LookRotation(moveDirection);
        }
        else
        {
            targetRotation = Quaternion.Euler(0, cameraYaw, 0);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, 
            targetRotation, rotationSpeed * deltaTime);
    }

    private bool IsAnyMovementKeyPressed()
    {
        foreach (KeyCode key in MovementKeys)
        {
            if (Input.GetKey(key)) return true;
        }
        return false;
    }

    private void DampenMovement()
    {
        moveVelocity = Vector3.Lerp(moveVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }

    private void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    private void RegenerateStamina()
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
    }

    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }
}