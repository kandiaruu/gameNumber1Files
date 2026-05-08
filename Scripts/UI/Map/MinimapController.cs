using UnityEngine;

//
// Follows the player with an orthographic top-down camera to render the minimap.
// Supports runtime zoom adjustment via keyboard input and persists zoom level across sessions.
//

public class MinimapController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform player;
    public float cameraHeight = 50f;

    [Header("Zoom Settings")]
    public float zoomStep = 25f;
    public float minZoom = 25f;
    public float maxZoom = 250f;

    public static float SavedZoom = 150f;

    private Camera cam;

    // Initializes the orthographic camera and applies the saved zoom level
    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        cam.orthographicSize = SavedZoom;
    }

    // Moves the camera above the player each frame and polls keyboard input for zoom adjustment
    private void LateUpdate()
    {
        if (player == null) return;

        transform.position = new Vector3(player.position.x, cameraHeight, player.position.z);

        if (Input.GetKeyDown(KeyCode.Equals)) AdjustZoom(-zoomStep);
        if (Input.GetKeyDown(KeyCode.Minus)) AdjustZoom(zoomStep);
    }

    // Changes the camera's orthographic size by delta, clamped between min and max zoom, and persists the new value
    private void AdjustZoom(float delta)
    {
        SavedZoom = Mathf.Clamp(cam.orthographicSize + delta, minZoom, maxZoom);
        cam.orthographicSize = SavedZoom;
    }
}
