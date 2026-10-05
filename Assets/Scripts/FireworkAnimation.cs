using System.Collections;
using UnityEngine;

public class FireworkAnimation : MonoBehaviour
{
    [SerializeField] private float startScale = 0.6f;
    [SerializeField] private float endScale = 1.15f;
    [SerializeField] private float duration = 1.2f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        StartCoroutine(AnimateFirework());
    }

    private IEnumerator AnimateFirework()
    {
        transform.localScale =
            Vector3.one * startScale;

        if (spriteRenderer != null)
        {
            Color c = originalColor;
            c.a = 0f;
            spriteRenderer.color = c;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth expansion.
            float smoothT =
                t * t * (3f - 2f * t);

            transform.localScale =
                Vector3.Lerp(
                    Vector3.one * startScale,
                    Vector3.one * endScale,
                    smoothT
                );

            if (spriteRenderer != null)
            {
                Color c = originalColor;

                // Fade in quickly, then fade out.
                float alpha;

                if (t < 0.2f)
                    alpha = t / 0.2f;
                else
                    alpha = 1f - ((t - 0.2f) / 0.8f);

                c.a = Mathf.Clamp01(alpha);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        if (spriteRenderer != null)
        {
            Color c = originalColor;
            c.a = 0f;
            spriteRenderer.color = c;
        }
    }
}