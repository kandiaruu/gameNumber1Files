using UnityEngine;
using UnityEngine.UI;

public class ButtonAlphaManager : MonoBehaviour
{
    [SerializeField] private float alphaThreshold = 0.1f; // Единый порог для всех кнопок
    [SerializeField] private string exclusionTag = "NoAlphaThreshold"; // Тег для исключения

    private Image[] cachedImages; // Кэшируем результаты

    void Awake()
    {
        // Кэшируем все Image на сцене без сортировки
        cachedImages = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
    }

    void Start()
    {
        // Применяем порог только к кнопкам без исключительного тега
        foreach (Image image in cachedImages)
        {
            if (image != null && image.GetComponent<Button>() != null && image.gameObject.tag != exclusionTag)
            {
                image.alphaHitTestMinimumThreshold = alphaThreshold;
            }
        }
    }
}