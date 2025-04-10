using UnityEngine;
using UnityEngine.UI;

public class ButtonAlphaManager : MonoBehaviour
{
    [SerializeField] private float alphaThreshold = 0.1f; // Единый порог для всех кнопок
    [SerializeField] private string exclusionTag = "NoAlphaThreshold"; // Тег для исключения

    private Image[] cachedImages; // Кэшируем результаты

    void Awake()
    {
        // Кэшируем все Image, включая неактивные объекты
        cachedImages = Resources.FindObjectsOfTypeAll<Image>();
    }

    void Start()
    {
        // Применяем порог только к кнопкам без исключительного тега
        foreach (Image image in cachedImages)
        {
            // Проверяем, что image не null и принадлежит сцене
            if (image == null || !image.gameObject.scene.IsValid())
                continue;

            if (image.GetComponent<Button>() != null && image.gameObject.tag != exclusionTag)
            {
                try
                {
                    image.alphaHitTestMinimumThreshold = alphaThreshold;
                }
                catch (System.Exception)
                {
                    
                }
            }
        }
    }
}