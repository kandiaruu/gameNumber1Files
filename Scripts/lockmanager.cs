using UnityEngine;
using UnityEngine.EventSystems;

public class LockManager : MonoBehaviour
{
    [SerializeField] private skilltree skillTree; // Ссылка на skilltree для проверки состояния doubleJumpLock
    [SerializeField] private float originalScale = 0.55f; // Начальный масштаб
    [SerializeField] private float hoverScale = 0.6f;    // Масштаб при наведении

    private GameObject currentHoveredLock = null; // Текущий замок под курсором

    void Start()
    {
        if (skillTree == null)
        {
            skillTree = Object.FindFirstObjectByType<skilltree>();
            if (skillTree == null)
            {
                Debug.LogError("LockManager: Не удалось найти skilltree в сцене!");
            }
        }
    }

    // Вызывается, когда курсор наводится на любой замок
    public void OnLockPointerEnter(GameObject lockObject)
    {
        currentHoveredLock = lockObject;
        if (skillTree != null && skillTree.doubleJumpLock != null && skillTree.doubleJumpLock.activeSelf)
        {
            lockObject.transform.localScale = new Vector3(hoverScale, hoverScale, hoverScale);
            Debug.Log($"LockManager: Курсор наведён на {lockObject.name}, масштаб увеличен до {hoverScale}.");
        }
    }

    // Вызывается, когда курсор уходит с замка
    public void OnLockPointerExit(GameObject lockObject)
    {
        if (currentHoveredLock == lockObject)
        {
            currentHoveredLock = null;
            if (skillTree != null && skillTree.doubleJumpLock != null && skillTree.doubleJumpLock.activeSelf)
            {
                lockObject.transform.localScale = new Vector3(originalScale, originalScale, originalScale);
                Debug.Log($"LockManager: Курсор ушёл с {lockObject.name}, масштаб возвращён к {originalScale}.");
            }
        }
    }
}