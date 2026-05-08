using UnityEngine;

//
// Spawns floating damage number popups above world-space targets 
// and centered text messages directly onto a UI canvas.
//

public class DamagePopupSpawner : MonoBehaviour
{
    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private Canvas popupCanvas;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 2f, 0f);

    // Instantiates a damage popup above the target, styled by damage type and whether the hit was critical
    public void ShowDamage(Transform target, float damage, bool isCrit, DamageType damageType)
    {
        if (popupPrefab == null || popupCanvas == null || target == null) return;

        DamagePopup popup = Instantiate(popupPrefab, popupCanvas.transform, false);
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        popup.Init(damage, isCrit, damageType, target, defaultOffset, cam);
    }

    // Instantiates a text message popup directly in the center of the screen
    public void ShowMessage(string message, Color color, float duration = 2f)
    {
        if (popupPrefab == null || popupCanvas == null) return;

        DamagePopup popup = Instantiate(popupPrefab, popupCanvas.transform, false);
        popup.InitMessage(message, color, duration);
    }
}