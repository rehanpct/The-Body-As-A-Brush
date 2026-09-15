using UnityEngine;

public class ThemeBackgroundManager : MonoBehaviour
{
    [Header("Backgrounds")]
    public GameObject underwaterBackground;
    public GameObject taiwanBackground;

    private ThemeManager.Theme lastTheme;

    void Start()
    {
        if (ThemeManager.Instance == null)
        {
            Debug.LogError(
                "ThemeBackgroundManager: " +
                "ThemeManager is missing!"
            );

            return;
        }

        // Store the starting theme.
        lastTheme =
            ThemeManager.Instance.CurrentTheme;

        UpdateBackground();
    }

    void Update()
    {
        if (ThemeManager.Instance == null)
            return;

        ThemeManager.Theme currentTheme =
            ThemeManager.Instance.CurrentTheme;

        // Only update when the theme actually changes.
        if (currentTheme != lastTheme)
        {
            lastTheme = currentTheme;

            UpdateBackground();
        }
    }

    void UpdateBackground()
    {
        if (ThemeManager.Instance == null)
            return;

        if (ThemeManager.Instance.IsTaiwan())
        {
            ShowTaiwanBackground();
        }
        else
        {
            ShowUnderwaterBackground();
        }
    }

    void ShowTaiwanBackground()
    {
        if (taiwanBackground != null)
        {
            taiwanBackground.SetActive(true);
        }

        if (underwaterBackground != null)
        {
            underwaterBackground.SetActive(false);
        }

        Debug.Log(
            "🇹🇼 Taiwan background activated."
        );
    }

    void ShowUnderwaterBackground()
    {
        if (underwaterBackground != null)
        {
            underwaterBackground.SetActive(true);
        }

        if (taiwanBackground != null)
        {
            taiwanBackground.SetActive(false);
        }

        Debug.Log(
            "🌊 Underwater background activated."
        );
    }
}