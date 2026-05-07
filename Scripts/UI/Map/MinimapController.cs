using UnityEngine;

public class MinimapController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform player;
    public float cameraHeight = 50f;

    [Header("Zoom Settings")]
    public float zoomStep = 25f;
    public float minZoom = 25f;
    public float maxZoom = 250f;
    
    // ВАЖНО: Ставим 150f как значение по умолчанию здесь
    public static float SavedZoom = 150f; 

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        // При старте СРАЗУ ставим тот зум, который мы указали в переменной
        cam.orthographicSize = SavedZoom;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        transform.position = new Vector3(player.position.x, cameraHeight, player.position.z);

        if (Input.GetKeyDown(KeyCode.Equals)) AdjustZoom(-zoomStep);
        if (Input.GetKeyDown(KeyCode.Minus)) AdjustZoom(zoomStep);
    }

    private void AdjustZoom(float delta)
    {
        // Обновляем статическую переменную, чтобы MapPanel её видела
        SavedZoom = Mathf.Clamp(cam.orthographicSize + delta, minZoom, maxZoom);
        cam.orthographicSize = SavedZoom;
    }
}