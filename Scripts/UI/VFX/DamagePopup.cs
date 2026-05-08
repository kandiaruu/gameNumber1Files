using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

//
// Floating UI element that displays a damage number above a world-space target,
// or a centered text message on the screen. Drifts upward and fades out.
//

public enum DamageType
{
    Divine,
    Physical,
    Pure,
    Magical
}

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private Image critIcon;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float floatUpSpeed = 25f;

    private Camera cam;
    private Transform target;
    private Vector3 worldOffset;
    private bool isScreenCentered = false;
    private RectTransform rectTransform;

    // Initializes the RectTransform component
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Sets up the popup to display a numeric damage value with type-based coloring and an optional crit icon
    public void Init(float damage, bool isCrit, DamageType damageType, Transform target, Vector3 offset, Camera cameraRef)
    {
        this.target = target;
        this.worldOffset = offset;
        this.cam = cameraRef;
        this.isScreenCentered = false;

        damageText.text = damage.ToString("0.##");
        damageText.color = GetTypeColor(damageType);

        if (critIcon != null) critIcon.gameObject.SetActive(isCrit);

        if (canvasGroup != null) canvasGroup.alpha = 1f;

        StartCoroutine(FadeAndDestroy());
    }

    // Sets up the popup to display a centered screen message with a custom color and duration
    public void InitMessage(string message, Color color, float duration = 2f)
    {
        this.isScreenCentered = true;
        this.lifetime = duration;

        damageText.text = message;
        damageText.color = color;

        if (critIcon != null) critIcon.gameObject.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (rectTransform != null)
        {
            // Anchors the text exactly to the center of the UI canvas
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        StartCoroutine(FadeAndDestroy());
    }

    // Converts the target's world position to screen space, or moves the UI element up if it is screen-centered
    private void Update()
    {
        if (isScreenCentered)
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += Vector2.up * (floatUpSpeed * 5f * Time.deltaTime);
            }
            return;
        }

        if (target == null || cam == null) return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        transform.position = screenPos;

        worldOffset += Vector3.up * (floatUpSpeed * Time.deltaTime * 0.01f);
    }

    // Linearly fades out the canvas group over the popup's lifetime, then destroys the GameObject
    private IEnumerator FadeAndDestroy()
    {
        float t = 0f;
        while (t < lifetime)
        {
            t += Time.deltaTime;
            float k = 1f - (t / lifetime);
            if (canvasGroup != null) canvasGroup.alpha = k;
            yield return null;
        }

        Destroy(gameObject);
    }

    // Returns the display color associated with the given damage type
    private Color GetTypeColor(DamageType type)
    {
        switch (type)
        {
            case DamageType.Divine:   return Color.white;
            case DamageType.Physical: return new Color(204f / 255f, 85f / 255f, 0f, 1f);
            case DamageType.Pure:     return new Color(1f, 0.84f, 0f, 1f);
            case DamageType.Magical:  return new Color32(214, 167, 226, 255);
            default:                  return Color.white;
        }
    }
}