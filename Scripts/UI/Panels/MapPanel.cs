using UnityEngine;
using UnityEngine.EventSystems; // Нужно для проверки, не кликаем ли мы по кнопкам UI

public class MapPanel : BasePanel
{
    [Header("References")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private MinimapController followScript; // Перетащи сюда камеру (где висит скрипт слежения)

    [Header("Zoom Settings")]
    [SerializeField] private float minFullMapSize = 50f;
    [SerializeField] private float maxFullMapSize = 1000f;
    [SerializeField] private float zoomSensitivity = 50f;

    [Header("Panning Settings")]
    [SerializeField] private float dragSpeed = 2f;
    
    private Vector3 dragOrigin;
    private bool isDragging;

    public override void Open()
    {
        base.Open();
        if (followScript != null) followScript.enabled = false; // Выключаем слежку за игроком
        
        // Ставим камеру над игроком в момент открытия
        if (mapCamera != null && followScript.player != null)
        {
            Vector3 pPos = followScript.player.position;
            mapCamera.transform.position = new Vector3(pPos.x, mapCamera.transform.position.y, pPos.z);
        }
    }

    public override void Close()
    {
        if (followScript != null) followScript.enabled = true; // Возвращаем слежку
        base.Close();
    }

    private void Update()
    {
        if (!IsOpen) return;

        HandleZoom();
        HandlePanning();
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float newSize = mapCamera.orthographicSize - scroll * zoomSensitivity;
            mapCamera.orthographicSize = Mathf.Clamp(newSize, minFullMapSize, maxFullMapSize);
        }
    }

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
            
            // ИСПРАВЛЕНО: Поменяли местами currentMousePos и dragOrigin, 
            // чтобы убрать инверсию (тянешь вниз = карта едет вниз)
            Vector3 difference = currentMousePos - dragOrigin; 
            
            // Читаем DPI (чувствительность) из настроек. Если нет, берем 1f.
            float dpiMultiplier = PlayerPrefs.GetFloat("MapDragDPI", 1f);

            // Считаем скорость: базовая * фактор зума * DPI
            float moveFactor = (mapCamera.orthographicSize / 500f) * dragSpeed * dpiMultiplier;

            // Двигаем камеру
            Vector3 move = new Vector3(difference.x * moveFactor, 0, difference.y * moveFactor);
            mapCamera.transform.Translate(move, Space.World);

            dragOrigin = currentMousePos;
        }
    }
}