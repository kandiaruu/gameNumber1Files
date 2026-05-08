//
// Full-screen map panel that disables the minimap follow script while open.
// Supports orthographic zoom via scroll wheel and free-look panning via left-mouse drag.
//

using UnityEngine;
using UnityEngine.EventSystems;

public class MapPanel : BasePanel
{
    [Header("References")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private MinimapController followScript;

    [Header("Zoom Settings")]
    [SerializeField] private float minFullMapSize = 50f;
    [SerializeField] private float maxFullMapSize = 1000f;
    [SerializeField] private float zoomSensitivity = 50f;

    [Header("Panning Settings")]
    [SerializeField] private float dragSpeed = 2f;

    private Vector3 dragOrigin;
    private bool isDragging;

    // Disables the minimap follow script and centres the map camera over the player
    public override void Open()
    {
        base.Open();
        if (followScript != null) followScript.enabled = false;

        if (mapCamera != null && followScript.player != null)
        {
            Vector3 pPos = followScript.player.position;
            mapCamera.transform.position = new Vector3(pPos.x, mapCamera.transform.position.y, pPos.z);
        }
    }

    // Re-enables the minimap follow script when the map is closed
    public override void Close()
    {
        if (followScript != null) followScript.enabled = true;
        base.Close();
    }

    // Each frame, processes zoom and panning input while the panel is open
    private void Update()
    {
        if (!IsOpen) return;

        HandleZoom();
        HandlePanning();
    }

    // Adjusts the camera's orthographic size based on the scroll wheel, clamped to min/max limits
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float newSize = mapCamera.orthographicSize - scroll * zoomSensitivity;
            mapCamera.orthographicSize = Mathf.Clamp(newSize, minFullMapSize, maxFullMapSize);
        }
    }

    // Translates the map camera in world space based on left-mouse drag delta, scaled by zoom level and DPI setting
    private void HandlePanning()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            isDragging = true;
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 difference = currentMousePos - dragOrigin;

            float dpiMultiplier = PlayerPrefs.GetFloat("MapDragDPI", 1f);
            float moveFactor = (mapCamera.orthographicSize / 500f) * dragSpeed * dpiMultiplier;

            Vector3 move = new Vector3(difference.x * moveFactor, 0, difference.y * moveFactor);
            mapCamera.transform.Translate(move, Space.World);

            dragOrigin = currentMousePos;
        }
    }
}
