using UnityEngine;

public class ThirdPersonCharacter : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f; // Скорость обычного движения
    [SerializeField] private float sprintSpeed = 10f; // Скорость бега
    [SerializeField] private float rotationSpeed = 10f; // Скорость поворота
    [SerializeField] private float jumpForce = 10f; // Сила прыжка
    [SerializeField] private float groundCheckDistance = 0.2f; // Расстояние проверки земли
    [SerializeField] private float maxStamina = 100f; // Максимальная выносливость
    [SerializeField] private float staminaDrainRate = 20f; // Скорость расхода выносливости при беге
    [SerializeField] private float staminaRegenRate = 1.67f; // Скорость восстановления выносливости
    [SerializeField] private float staminaRegenDelay = 15f; // Задержка перед началом восстановления

    public ThirdPersonCamera cameraController; // Ссылка на контроллер камеры
    private Rigidbody rb; // Компонент физики
    private InventoryManager inventoryManager; // Ссылка на менеджер инвентаря
    private Vector3 moveVelocity; // Вектор скорости движения
    private bool isGrounded; // Находится ли персонаж на земле
    private float currentStamina; // Текущая выносливость
    private float timeSinceLastSprint; // Время с последнего бега

    private static readonly Vector3 GroundCheckOffset = Vector3.up * 0.1f; // Смещение для проверки земли
    private static readonly KeyCode[] MovementKeys = { KeyCode.A, KeyCode.S, KeyCode.D }; // Клавиши движения

    private void Awake()
    {
        InitializeComponents();
        currentStamina = maxStamina; // Инициализация выносливости
        timeSinceLastSprint = staminaRegenDelay; // Установка начального времени для восстановления
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) Debug.LogError("Не найден компонент Rigidbody!");

        cameraController ??= Object.FindFirstObjectByType<ThirdPersonCamera>();
        if (!cameraController) Debug.LogError("Не найдена ThirdPersonCamera!");

        inventoryManager ??= Object.FindFirstObjectByType<InventoryManager>();
        if (!inventoryManager) Debug.LogError("Не найден InventoryManager!");
    }

    private void Update()
    {
        // Логика восстановления выносливости работает всегда, даже если инвентарь открыт
        if (!Input.GetKey(KeyCode.LeftShift) || currentStamina <= 0)
        {
            timeSinceLastSprint += Time.deltaTime;
            if (timeSinceLastSprint >= staminaRegenDelay)
            {
                RegenerateStamina();
            }
        }

        // Управление прыжком и движением отключается при открытом инвентаре
        if (inventoryManager?.IsInventoryOpen ?? false) return;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        bool isInventoryOpen = inventoryManager?.IsInventoryOpen ?? false;
        isGrounded = Physics.Raycast(transform.position + GroundCheckOffset, 
            Vector3.down, groundCheckDistance + 0.1f);

        Vector3 inputDirection = GetInputDirection();
        
        if (!isInventoryOpen)
        {
            Move(inputDirection);
            Rotate(inputDirection);
        }
        else
        {
            DampenMovement();
            SyncRotationWithCamera();
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
        else
        {
            moveVelocity = Vector3.Lerp(moveVelocity, Vector3.zero, deltaTime * 5f);
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

    private void SyncRotationWithCamera()
    {
        float deltaTime = Time.fixedDeltaTime;
        Quaternion targetRotation = Quaternion.Euler(0, cameraController.yaw, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, 
            targetRotation, rotationSpeed * deltaTime);
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