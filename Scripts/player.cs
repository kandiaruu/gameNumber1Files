using UnityEngine;

public class ThirdPersonCharacter : MonoBehaviour, IThirdPersonCharacter
{
    [SerializeField] private GameObject eKeyIcon;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    public ThirdPersonCamera cameraController;
    private Rigidbody rb;
    private bool isGrounded;

    private static readonly Vector3 GroundCheckOffset = Vector3.up * 0.1f;

    [InjectAttribute1] private IUIManager uiManager { get; set; }
    [InjectAttribute1] private IChestUIController chestUIController { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) Debug.LogError("No Rigidbody component found!");
    }

    private void Update()
    {
        CheckForChestAndToggleIcon();
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryOpenNearbyChest();
        }
        // Jump handling
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    private void CheckForChestAndToggleIcon()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Ray ray = cameraController.GetComponent<Camera>().ScreenPointToRay(screenCenter);
        RaycastHit hit;
        float maxDistance = 10f;

        bool lookingAtChest = false;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            if (hit.collider.TryGetComponent(out Chest chest))
            {
                lookingAtChest = true;
            }
        }

        // Включаем или выключаем иконку
        if (eKeyIcon != null)
            eKeyIcon.SetActive(lookingAtChest);
    }

    void TryOpenNearbyChest()
    {
        // Берём центр экрана (например, для UI точки по центру, как прицел)
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

        // Получаем луч из центра экрана в мире
        Ray ray = cameraController.GetComponent<Camera>().ScreenPointToRay(screenCenter);
        RaycastHit hit;

        // Максимальная дистанция проверки, например 2f (можно увеличить/уменьшить по желанию)
        float maxDistance = 10f;

        // Проверяем, попали ли мы в сундук
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            if (hit.collider.TryGetComponent(out Chest chest))
            {
                uiManager.OpenPanel(UIManager.PanelType.Inventory);
                chestUIController.OpenChestUI(chest);
            }
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