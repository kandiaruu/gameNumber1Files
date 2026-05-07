using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
public enum DamageType
{
    Divine,     // белый
    Physical,   // серый
    Pure,       // золотой
    Magical     // rgb(214, 167, 226)
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

    public void Init(float damage, bool isCrit, DamageType damageType, Transform target, Vector3 offset, Camera cameraRef)
    {
        this.target = target;
        this.worldOffset = offset;
        this.cam = cameraRef;

        // только число
        damageText.text = damage.ToString("0.##");

        // цвет всегда от типа урона (и для крита тоже)
        damageText.color = GetTypeColor(damageType);

        // иконка крита отдельно
        if (critIcon != null) critIcon.gameObject.SetActive(isCrit);

        if (canvasGroup != null) canvasGroup.alpha = 1f;

        StartCoroutine(FadeAndDestroy());
    }

    public void InitMessage(string message, Color color, Transform target, Vector3 offset, Camera cameraRef)
    {
        this.target = target;
        this.worldOffset = offset;
        this.cam = cameraRef;

        damageText.text = message;
        damageText.color = color;
        
        if (critIcon != null) critIcon.gameObject.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        StartCoroutine(FadeAndDestroy());
    }

    private void Update()
    {
        if (target == null || cam == null) return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        transform.position = screenPos;

        worldOffset += Vector3.up * (floatUpSpeed * Time.deltaTime * 0.01f);
    }

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

    private Color GetTypeColor(DamageType type)
    {
        switch (type)
        {
            case DamageType.Divine:   return Color.white;                         // белый
            case DamageType.Physical: return new Color(204f / 255f, 85f / 255f, 0f, 1f);  // серый
            case DamageType.Pure:     return new Color(1f, 0.84f, 0f, 1f);        // золотой
            case DamageType.Magical:  return new Color32(214, 167, 226, 255);     // rgb(214,167,226)
            default:                  return Color.white;
        }
    }
}