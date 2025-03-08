using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LockScalerComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private LockManager lockManager; // Ссылка на менеджер
    //private float alphaThreshold = 0.1f;

    void Start()
    {
        //this.GetComponent<Image>().alphaHitTestMinimumThreshold = alphaThreshold;

        if (lockManager == null)
        {
            lockManager = Object.FindFirstObjectByType<LockManager>();
            if (lockManager == null)
            {
                Debug.LogError("LockScalerComponent: Не удалось найти LockManager в сцене!");
            }
        }
        // Устанавливаем начальный масштаб (можно настроить в инспекторе)
        transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (lockManager != null)
        {
            lockManager.OnLockPointerEnter(gameObject);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (lockManager != null)
        {
            lockManager.OnLockPointerExit(gameObject);
        }
    }
}