using UnityEngine;

public class DamagePopupSpawner : MonoBehaviour
{
    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private Canvas popupCanvas;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 2f, 0f);

    public void ShowDamage(Transform target, float damage, bool isCrit, DamageType damageType)
    {
        if (popupPrefab == null || popupCanvas == null || target == null) return;

        DamagePopup popup = Instantiate(popupPrefab, popupCanvas.transform);
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        popup.Init(damage, isCrit, damageType, target, defaultOffset, cam);
    }
}