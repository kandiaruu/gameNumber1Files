using UnityEngine;
using UnityEngine.UI;

//
// Sets a minimum alpha threshold on all Button Image components at startup,
// so that fully transparent areas of a button sprite do not register clicks.
// Objects tagged with the exclusion tag are skipped.
//

public class ButtonAlphaManager : MonoBehaviour
{
    [SerializeField] private float alphaThreshold = 0.1f;
    [SerializeField] private string exclusionTag = "NoAlphaThreshold";

    private Image[] cachedImages;

    // Caches all Image components in the project, including those on inactive objects
    void Awake()
    {
        cachedImages = Resources.FindObjectsOfTypeAll<Image>();
    }

    // Applies the alpha hit-test threshold to every Button Image that belongs to a valid scene and is not excluded by tag
    void Start()
    {
        foreach (Image image in cachedImages)
        {
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
