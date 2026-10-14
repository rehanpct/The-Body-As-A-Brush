
using UnityEngine;
using UnityEngine.UI;

public class DrawingSceneGestureBar : MonoBehaviour
{
    [Header("Gesture Bar Image")]
    [SerializeField] private Image targetImage;

    [Header("Underwater Theme")]
    [SerializeField] private Sprite underwaterEnglish;
    [SerializeField] private Sprite underwaterChinese;

    [Header("Taiwan Theme")]
    [SerializeField] private Sprite taiwanEnglish;
    [SerializeField] private Sprite taiwanChinese;

    private void OnEnable()
    {
        LanguageManager.LanguageChanged += RefreshGestureBar;
        RefreshGestureBar();
    }

    private void OnDisable()
    {
        LanguageManager.LanguageChanged -= RefreshGestureBar;
    }

    private void RefreshGestureBar()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetImage == null)
        {
            Debug.LogError(
                "[DrawingSceneGestureBar] Target Image is not assigned."
            );
            return;
        }

        if (ThemeManager.Instance == null)
        {
            Debug.LogWarning(
                "[DrawingSceneGestureBar] ThemeManager.Instance is missing."
            );
            return;
        }

        bool isChinese = LanguageManager.IsTraditionalChinese;
        Sprite selectedSprite;

        switch (ThemeManager.Instance.CurrentTheme)
        {
            case ThemeManager.Theme.Underwater:
                selectedSprite = isChinese
                    ? underwaterChinese
                    : underwaterEnglish;
                break;

            case ThemeManager.Theme.Taiwan:
                selectedSprite = isChinese
                    ? taiwanChinese
                    : taiwanEnglish;
                break;

            default:
                Debug.LogWarning(
                    "[DrawingSceneGestureBar] Unknown theme."
                );
                return;
        }

        if (selectedSprite == null)
        {
            Debug.LogWarning(
                "[DrawingSceneGestureBar] Assign the sprite for " +
                ThemeManager.Instance.CurrentTheme +
                " / " +
                LanguageManager.CurrentLanguage
            );
            return;
        }

        targetImage.sprite = selectedSprite;

        Debug.Log(
            "[DrawingSceneGestureBar] Updated: " +
            ThemeManager.Instance.CurrentTheme +
            " / " +
            LanguageManager.CurrentLanguage
        );
    }
}
