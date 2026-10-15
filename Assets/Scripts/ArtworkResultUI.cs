
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ArtworkResultUI : MonoBehaviour
{
    [Header("Existing Scene References")]
    public RawImage artworkPreview;
    public TMP_Text objectsValue;
    public TMP_Text gesturesValue;
    public TMP_Text timeValue;
    public TMP_Text feedbackText;

    [Header("Existing Taiwan Layout References")]
    [SerializeField] private RawImage taiwanArtworkPreview;
    [SerializeField] private TMP_Text taiwanObjectsValue;
    [SerializeField] private TMP_Text taiwanGesturesValue;
    [SerializeField] private TMP_Text taiwanTimeValue;
    [SerializeField] private TMP_Text taiwanFeedbackText;

    [Header("Scene Navigation")]
    [SerializeField] private string drawingSceneName = "DrawingScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private ArtworkResultData result;
    private Texture2D loadedArtworkTexture;

    private void OnEnable() { LanguageManager.LanguageChanged += RefreshFeedbackLanguage; }
    private void OnDisable() { LanguageManager.LanguageChanged -= RefreshFeedbackLanguage; }
    private void RefreshFeedbackLanguage()
    {
        if (result != null)
            DisplayAIFeedback();
    }

    private void Start()
    {
        LoadResultData();
    }

    private void OnDestroy()
    {
        if (artworkPreview != null &&
            artworkPreview.texture == loadedArtworkTexture)
        {
            artworkPreview.texture = null;
        }

        if (loadedArtworkTexture != null)
        {
            Destroy(loadedArtworkTexture);
            loadedArtworkTexture = null;
        }
    }

    private void LoadResultData()
    {
        if (ArtworkResultManager.Instance == null)
        {
            Debug.LogError(
                "ArtworkResultUI: ArtworkResultManager is missing."
            );
            return;
        }

        if (!ArtworkResultManager.Instance.HasResult())
        {
            Debug.LogError(
                "ArtworkResultUI: No artwork result is available."
            );
            return;
        }

        result = ArtworkResultManager.Instance.CurrentResult;

        if (result == null || result.artworkData == null)
        {
            Debug.LogError(
                "ArtworkResultUI: Result or ArtworkData is missing."
            );
            return;
        }

        if (ArtworkResultThemeSwitcher.IsTaiwanTheme(result.theme))
        {
            artworkPreview = taiwanArtworkPreview;
            objectsValue = taiwanObjectsValue;
            gesturesValue = taiwanGesturesValue;
            timeValue = taiwanTimeValue;
            feedbackText = taiwanFeedbackText;
        }

        DisplayResult();
    }

    private void DisplayResult()
    {
        ArtworkData data = result.artworkData;

        // STATISTIC 1: Creation time
        SetText(
            timeValue,
            FormatTime(data.creationTime)
        );

        // STATISTIC 2: Total gestures used
        int gesturesUsed =
            data.indexGestureCount +
            data.openPalmGestureCount +
            data.vSignGestureCount +
            data.thumbUpGestureCount;

        SetText(
            gesturesValue,
            gesturesUsed.ToString()
        );

        // STATISTIC 3: Total elements created
        int totalElements =
            data.waterCount +
            data.coralCount +
            data.fishSchoolCount +
            data.bubbleBurstCount +
            data.lanternCount +
            data.lightTrailCount +
            data.petalsCount +
            data.fireworksCount;

        SetText(
            objectsValue,
            totalElements.ToString()
        );

        LoadArtworkImage();
        DisplayAIFeedback();

        Debug.Log(
            $"Artwork result displayed. Theme: {result.theme}, " +
            $"Time: {FormatTime(data.creationTime)}, " +
            $"Gestures: {gesturesUsed}, Elements: {totalElements}"
        );
    }

    private void DisplayAIFeedback()
    {
        if (feedbackText == null)
        {
            Debug.LogWarning(
                "ArtworkResultUI: Feedback Text is not assigned."
            );
            return;
        }

        string description =
            string.IsNullOrWhiteSpace(result.aiDescription) || result.feedbackLanguage != LanguageManager.CurrentLanguage
                ? (LanguageManager.IsTraditionalChinese ? "你的作品評析將在此顯示。" : "Your artwork interpretation will appear here.")
                : result.aiDescription;

        // Generated feedback is the display/speech source; do not prepend a fallback title.
        feedbackText.text = description;
        Debug.Log("[ResultUI] Language = " + LanguageManager.CurrentLanguage +
            "; Displayed text = " + feedbackText.text);
    }

    private void LoadArtworkImage()
    {
        if (artworkPreview == null)
        {
            Debug.LogWarning(
                "ArtworkResultUI: ArtworkPreview is not assigned."
            );
            return;
        }

        string imagePath = result.artworkImagePath;

        if (string.IsNullOrWhiteSpace(imagePath) ||
            !File.Exists(imagePath))
        {
            Debug.LogError(
                "ArtworkResultUI: Artwork image not found. Path: " +
                imagePath
            );
            return;
        }

        try
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);

            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false
            );

            if (!texture.LoadImage(imageBytes))
            {
                Debug.LogError(
                    "ArtworkResultUI: Could not decode artwork image."
                );
                Destroy(texture);
                return;
            }

            if (loadedArtworkTexture != null)
            {
                Destroy(loadedArtworkTexture);
            }

            loadedArtworkTexture = texture;
            artworkPreview.texture = loadedArtworkTexture;
            FitArtworkPreview();

            Debug.Log("Artwork preview loaded successfully.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "ArtworkResultUI: Image loading failed. " +
                exception.Message
            );
        }
    }

    private void FitArtworkPreview()
    {
        RectTransform preview = artworkPreview.rectTransform;
        RectTransform frame = preview.parent as RectTransform;
        if (frame == null || loadedArtworkTexture == null)
            return;

        // Match the frame sprite's rendered rectangle, including Preserve Aspect.
        // FrameOverlay draws the inward decorations above the fitted artwork.
        Vector2 available = frame.rect.size;
        Image frameImage = frame.GetComponent<Image>();
        if (frameImage != null && frameImage.preserveAspect && frameImage.sprite != null)
        {
            float frameAspect = frameImage.sprite.rect.width / frameImage.sprite.rect.height;
            if (available.x / available.y > frameAspect)
                available.x = available.y * frameAspect;
            else
                available.y = available.x / frameAspect;
        }
        // Fit inside the border rails; do not shrink to avoid the overlaid ornaments.
        available = Vector2.Scale(available, new Vector2(0.95f, 0.88f));
        available -= new Vector2(16f, 16f); // Add 8 px per edge inside the existing usable area.
        float scale = Mathf.Min(available.x / loadedArtworkTexture.width,
            available.y / loadedArtworkTexture.height);
        preview.anchorMin = preview.anchorMax = new Vector2(0.5f, 0.5f);
        preview.pivot = new Vector2(0.5f, 0.5f);
        preview.anchoredPosition = Vector2.zero;
        preview.sizeDelta = new Vector2(loadedArtworkTexture.width, loadedArtworkTexture.height) * scale;
    }

    public void SaveToAlbum()
    {
        if (result == null)
        {
            Debug.LogError(
                "ArtworkResultUI: Nothing is available to save."
            );
            return;
        }

        if (ArtworkAlbumManager.Instance == null)
        {
            Debug.LogError(
                "ArtworkResultUI: ArtworkAlbumManager is missing."
            );
            return;
        }

        bool saved =
            ArtworkAlbumManager.Instance.SaveArtwork(result);

        if (saved)
        {
            Debug.Log("Artwork saved to album successfully.");
        }
        else
        {
            Debug.LogError("Could not save artwork to album.");
        }
    }

    public void CreateAgain()
    {
        Debug.Log("Try Again pressed.");

        if (ArtworkResultManager.Instance != null)
        {
            ArtworkResultManager.Instance.ClearResult();
        }

        SceneManager.LoadScene(drawingSceneName);
    }

    public void BackToMenu()
    {
        Debug.Log("Back to Menu pressed.");

        if (ArtworkResultManager.Instance != null)
        {
            ArtworkResultManager.Instance.ClearResult();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetText(TMP_Text textComponent, string value)
    {
        if (textComponent != null)
        {
            textComponent.text = value;
        }
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(
            0,
            Mathf.FloorToInt(seconds)
        );

        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return minutes.ToString("00") + ":" +
               remainingSeconds.ToString("00");
    }
}
