using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float scrollSpeed = 1f;
    public float mouseSensitivity = 100f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 5f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private float pitch = 15f;

    public float yaw = 0f;
    public bool isSettingsOpen = false;

    private InventoryManager inventoryManager;
    private float yawVelocity = 0f;
    private float pitchVelocity = 0f;
    
    private Transform cachedTransform;
    private Vector3 targetPositionCache;

    private void Awake()
    {
        cachedTransform = transform;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        inventoryManager ??= Object.FindFirstObjectByType<InventoryManager>();
        if (!inventoryManager) Debug.LogError("InventoryManager not found!");
    }

    private void LateUpdate()
    {
        bool isInventoryOpen = inventoryManager?.IsInventoryOpen ?? false;
        float deltaTime = Time.deltaTime;

        if (!isInventoryOpen && !isSettingsOpen)
        {
            HandleInput(deltaTime);
        }

        UpdateRotation(deltaTime);
        UpdatePosition(deltaTime);
    }

    private void HandleInput(float deltaTime)
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            float change = scrollInput > 0 ? -scrollSpeed : scrollSpeed;
            distance = Mathf.Clamp(distance + change, minDistance, maxDistance);
            height = Mathf.Clamp(height + change, minHeight, maxHeight);
        }

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");
        
        yawVelocity = mouseX * mouseSensitivity;
        if (Input.GetMouseButton(1))
        {
            pitchVelocity = -mouseY * mouseSensitivity;
        }
    }

    private void UpdateRotation(float deltaTime)
    {
        yaw += yawVelocity * deltaTime;
        pitch = Mathf.Clamp(pitch + pitchVelocity * deltaTime, minPitch, maxPitch);

        yawVelocity = Mathf.Lerp(yawVelocity, 0f, deltaTime * smoothSpeed);
        pitchVelocity = Mathf.Lerp(pitchVelocity, 0f, deltaTime * smoothSpeed);
    }

    private void UpdatePosition(float deltaTime)
    {
        if (!target) return;

        targetPositionCache = target.position;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = new Vector3(0, height, -distance);
        Vector3 desiredPosition = targetPositionCache + rotation * offset;

        cachedTransform.position = Vector3.Lerp(cachedTransform.position, 
            desiredPosition, smoothSpeed * deltaTime);
        cachedTransform.rotation = rotation;
        cachedTransform.LookAt(targetPositionCache);
    }

    private void OnValidate()
    {
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        height = Mathf.Clamp(height, minHeight, maxHeight);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
}