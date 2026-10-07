using UnityEngine;
using UnityEngine.UI;

public class GestureBarThemeVisual : MonoBehaviour
{
    [Header("Gesture Bar Sprites")]
    [SerializeField] private Sprite underwaterSprite;
    [SerializeField] private Sprite taiwanSprite;

    [Header("Target Image")]
    [SerializeField] private Image targetImage;

    private ThemeManager.Theme lastTheme;

    private void Start()
    {
        if (ThemeManager.Instance == null)
        {
            Debug.LogError(
                "GestureBarThemeVisual: " +
                "ThemeManager is missing!"
            );

            return;
        }

        // Store the starting theme.
        lastTheme =
            ThemeManager.Instance.CurrentTheme;

        UpdateGestureBar();
    }

    private void Update()
    {
        if (ThemeManager.Instance == null)
            return;

        ThemeManager.Theme currentTheme =
            ThemeManager.Instance.CurrentTheme;

        // Only update when the theme actually changes.
        if (currentTheme != lastTheme)
        {
            lastTheme = currentTheme;

            UpdateGestureBar();
        }
    }

    private void UpdateGestureBar()
    {
        if (ThemeManager.Instance == null)
            return;

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage == null)
        {
            Debug.LogError(
                "GestureBarThemeVisual: " +
                "Target Image is missing!"
            );

            return;
        }

        if (ThemeManager.Instance.IsTaiwan())
        {
            targetImage.sprite = taiwanSprite;

            Debug.Log(
                "🇹🇼 Taiwan gesture bar activated."
            );
        }
        else
        {
            targetImage.sprite = underwaterSprite;

            Debug.Log(
                "🌊 Underwater gesture bar activated."
            );
        }

        targetImage.preserveAspect = true;
    }
}