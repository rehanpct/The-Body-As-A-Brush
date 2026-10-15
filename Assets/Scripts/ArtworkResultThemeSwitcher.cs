
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

        bool isTaiwan =
            selectedTheme.Trim().ToLowerInvariant().Contains("taiwan") ||
            selectedTheme.Trim().ToLowerInvariant().Contains("nightmarket");

        if (underwaterTheme != null)
            underwaterTheme.SetActive(!isTaiwan);

        if (taiwanTheme != null)
            taiwanTheme.SetActive(isTaiwan);

        Debug.Log(
            $"[ArtworkResultThemeSwitcher] Selected theme: {selectedTheme}. " +
            $"Showing: {(isTaiwan ? "Taiwan" : "Underwater")}"
        );
    }
}
