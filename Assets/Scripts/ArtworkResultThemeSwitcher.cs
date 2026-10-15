
using UnityEngine;

public class ArtworkResultThemeSwitcher : MonoBehaviour
{
    [Header("Theme Layouts")]
    [SerializeField] private GameObject underwaterTheme;
    [SerializeField] private GameObject taiwanTheme;

    private void Start()
    {
        ApplySelectedTheme();
    }

    public void ApplySelectedTheme()
    {
        string selectedTheme =
            PlayerPrefs.GetString("SelectedTheme", "Underwater");

        if (ArtworkResultManager.Instance != null && ArtworkResultManager.Instance.HasResult())
            selectedTheme = ArtworkResultManager.Instance.CurrentResult.theme;

        bool isTaiwan = IsTaiwanTheme(selectedTheme);

        if (underwaterTheme != null)
            underwaterTheme.SetActive(!isTaiwan);

        if (taiwanTheme != null)
            taiwanTheme.SetActive(isTaiwan);

        Debug.Log(
            $"[ArtworkResultThemeSwitcher] Selected theme: {selectedTheme}. " +
            $"Showing: {(isTaiwan ? "Taiwan" : "Underwater")}"
        );
    }

    public static bool IsTaiwanTheme(string theme)
    {
        string normalized = (theme ?? "").Trim().ToLowerInvariant();
        return normalized.Contains("taiwan") || normalized.Contains("nightmarket");
    }
}
