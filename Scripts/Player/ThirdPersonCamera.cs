using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 fpvOffset = new Vector3(0f, 1.7f, 0.1f); // Head position
    
    [Tooltip("Базовая чувствительность мыши")]
    [SerializeField] private float mouseSensitivity = 1.2f;
    
    [SerializeField] private float pitchMin = -60f;
    [SerializeField] private float pitchMax = 80f;
    public bool isSettingsOpen = false;

    public float yaw = 0f;
    public float pitch = 0f;

    private void Awake()
    {
        if (!target)
        {
            Debug.LogError("ThirdPersonCamera: Target not assigned!");
        }
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    private void LateUpdate()
    {
        if (!target) return;

        HandleInput();
        UpdateCamera();
    }

    private void HandleInput()
    {
        if (isSettingsOpen) return;

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        // <--- ДОБАВЛЕНО: Читаем множитель из настроек меню --->
        float sensitivityMultiplier = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        float finalSensitivity = mouseSensitivity * sensitivityMultiplier;

        // Применяем итоговую чувствительность
        yaw += mouseX * finalSensitivity;
        pitch = Mathf.Clamp(pitch - mouseY * finalSensitivity, pitchMin, pitchMax);
    }

    private void UpdateCamera()
    {
        // Set position to head (or custom offset)
        Vector3 fpvPosition = target.position + target.TransformVector(fpvOffset);
        transform.position = fpvPosition;

        // Set rotation by yaw (Y) and pitch (X)
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;
    }

    // For compatibility with previous code, always return true (only fpv)
    public bool IsFPV() => true;
}