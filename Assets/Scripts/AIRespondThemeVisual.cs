using UnityEngine;
using UnityEngine.UI;

public class AIRespondThemeVisual : MonoBehaviour
{
    [Header("Theme Sprites")]
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
                "AIRespondThemeVisual: " +
                "ThemeManager is missing!"
            );

            return;
        }

        lastTheme =
            ThemeManager.Instance.CurrentTheme;

        UpdateVisual();
    }

    private void Update()
    {
        if (ThemeManager.Instance == null)
            return;

        ThemeManager.Theme currentTheme =
            ThemeManager.Instance.CurrentTheme;

        if (currentTheme != lastTheme)
        {
            lastTheme = currentTheme;

            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage == null)
        {
            Debug.LogError(
                "AIRespondThemeVisual: " +
                "Target Image is missing!"
            );

            return;
        }

        if (ThemeManager.Instance.IsTaiwan())
        {
            targetImage.sprite = taiwanSprite;

            Debug.Log(
                "🇹🇼 Taiwan AI response icon activated."
            );
        }
        else
        {
            targetImage.sprite = underwaterSprite;

            Debug.Log(
                "🌊 Underwater AI response icon activated."
            );
        }

        targetImage.preserveAspect = true;
    }
}