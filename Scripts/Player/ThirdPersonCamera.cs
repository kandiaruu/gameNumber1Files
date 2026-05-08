//
// ThirdPersonCamera implements a first-person-view camera that follows a target transform.
// It reads mouse input each frame, applies a user-configurable sensitivity multiplier
// stored in PlayerPrefs, clamps vertical pitch, and positions/rotates the camera
// to a head-level offset on the target.
//

using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 fpvOffset = new Vector3(0f, 1.7f, 0.1f);

    [Tooltip("Base mouse sensitivity")]
    [SerializeField] private float mouseSensitivity = 1.2f;

    [SerializeField] private float pitchMin = -60f;
    [SerializeField] private float pitchMax = 80f;
    public bool isSettingsOpen = false;

    public float yaw = 0f;
    public float pitch = 0f;

    // Validates that a target is assigned and initialises yaw/pitch from the current transform rotation
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

    // Runs after all Updates each frame; skips processing if no target is assigned
    private void LateUpdate()
    {
        if (!target) return;

        HandleInput();
        UpdateCamera();
    }

    // Reads raw mouse axes, combines base sensitivity with the PlayerPrefs multiplier, and updates yaw/pitch
    private void HandleInput()
    {
        if (isSettingsOpen) return;

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        float sensitivityMultiplier = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        float finalSensitivity = mouseSensitivity * sensitivityMultiplier;

        yaw += mouseX * finalSensitivity;
        pitch = Mathf.Clamp(pitch - mouseY * finalSensitivity, pitchMin, pitchMax);
    }

    // Positions the camera at the head offset on the target and applies the current yaw/pitch rotation
    private void UpdateCamera()
    {
        Vector3 fpvPosition = target.position + target.TransformVector(fpvOffset);
        transform.position = fpvPosition;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;
    }

    // Always returns true; retained for compatibility with code that checks the camera mode
    public bool IsFPV() => true;
}
