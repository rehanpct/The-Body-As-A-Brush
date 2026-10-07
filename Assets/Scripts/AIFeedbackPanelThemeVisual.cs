using UnityEngine;
using UnityEngine.UI;

public class AIFeedbackPanelThemeVisual : MonoBehaviour
{
    [Header("Theme Panel Sprites")]
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
                "AIFeedbackPanelThemeVisual: " +
                "ThemeManager is missing!"
            );

            return;
        }

        lastTheme = ThemeManager.Instance.CurrentTheme;

        UpdatePanel();
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

            UpdatePanel();
        }
    }

    private void UpdatePanel()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage == null)
        {
            Debug.LogError(
                "AIFeedbackPanelThemeVisual: " +
                "Target Image is missing!"
            );

            return;
        }

        if (ThemeManager.Instance.IsTaiwan())
        {
            targetImage.sprite = taiwanSprite;

            Debug.Log(
                "🇹🇼 Taiwan AI feedback panel activated."
            );
        }
        else
        {
            targetImage.sprite = underwaterSprite;

            Debug.Log(
                "🌊 Underwater AI feedback panel activated."
            );
        }

        targetImage.preserveAspect = true;
    }
}