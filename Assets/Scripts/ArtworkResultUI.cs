
using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

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
    private byte[] loadedArtworkPng;
    private Coroutine finalAnalysis;
    private UnityWebRequest finalAnalysisRequest;
    private bool analyzingFinalArtwork;

    [Header("Final Artwork Analysis")]
    [SerializeField] private string finalArtworkFeedbackUrl =
        "https://the-body-as-a-brush.onrender.com/api/artwork-feedback";

    [Serializable]
    private class FinalArtworkRequest
    {
        public string sessionId;
        public string language;
        public string imageBase64;
    }

    [Serializable]
    private class FinalArtworkResponse
    {
        public string finalArtworkFeedback;
        public string language;
    }

    public string ReplayableFinalFeedback => !analyzingFinalArtwork && result != null &&
        result.finalFeedbackLanguage == LanguageManager.CurrentLanguage && feedbackText != null
        ? feedbackText.text : string.Empty;

    private void OnEnable()
    {
        LanguageManager.LanguageChanged += RefreshFeedbackLanguage;
        if (result != null) BeginFinalArtworkAnalysis();
    }
    private void OnDisable()
    {
        LanguageManager.LanguageChanged -= RefreshFeedbackLanguage;
        CancelFinalAnalysis();
    }
    private void RefreshFeedbackLanguage()
    {
        if (result != null)
            BeginFinalArtworkAnalysis();
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
        BeginFinalArtworkAnalysis();

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

        string description = !analyzingFinalArtwork &&
            result.finalFeedbackLanguage == LanguageManager.CurrentLanguage &&
            !string.IsNullOrWhiteSpace(result.finalArtworkFeedback)
                ? result.finalArtworkFeedback
                : (LanguageManager.IsTraditionalChinese ? "正在欣賞你的作品..." : "Analyzing your artwork...");

        // Generated feedback is the display/speech source; do not prepend a fallback title.
        feedbackText.text = description;
        Debug.Log("[ResultUI] Language = " + LanguageManager.CurrentLanguage +
            "; Displayed text = " + feedbackText.text);
    }

    private void CancelFinalAnalysis()
    {
        if (finalAnalysisRequest != null)
        {
            finalAnalysisRequest.Abort();
            finalAnalysisRequest.Dispose();
            finalAnalysisRequest = null;
        }
        if (finalAnalysis != null) StopCoroutine(finalAnalysis);
        finalAnalysis = null;
    }

    private void BeginFinalArtworkAnalysis()
    {
        CancelFinalAnalysis();
        if (!string.IsNullOrWhiteSpace(result.finalArtworkFeedback) &&
            result.finalFeedbackLanguage == LanguageManager.CurrentLanguage)
        {
            analyzingFinalArtwork = false;
            DisplayAIFeedback();
            return;
        }
        analyzingFinalArtwork = true;
        DisplayAIFeedback();
        finalAnalysis = StartCoroutine(AnalyzeFinalArtwork(LanguageManager.CurrentLanguage));
    }

    private IEnumerator AnalyzeFinalArtwork(string language)
    {
        if (loadedArtworkPng == null || loadedArtworkPng.Length == 0)
        {
            CompleteFinalAnalysis(null, language, "The final preview PNG could not be loaded: " + result.artworkImagePath);
            yield break;
        }
        UnityWebRequestAsyncOperation operation;
        try
        {
            string json = JsonUtility.ToJson(new FinalArtworkRequest
            {
                sessionId = Guid.NewGuid().ToString("N"),
                language = language,
                imageBase64 = Convert.ToBase64String(loadedArtworkPng)
            });
            finalAnalysisRequest = new UnityWebRequest(finalArtworkFeedbackUrl, "POST");
            finalAnalysisRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            finalAnalysisRequest.downloadHandler = new DownloadHandlerBuffer();
            finalAnalysisRequest.SetRequestHeader("Content-Type", "application/json");
            finalAnalysisRequest.timeout = 90;
            operation = finalAnalysisRequest.SendWebRequest();
        }
        catch (Exception exception)
        {
            if (finalAnalysisRequest != null) finalAnalysisRequest.Dispose();
            finalAnalysisRequest = null;
            CompleteFinalAnalysis(null, language, exception.Message);
            yield break;
        }
        Debug.Log("[FinalArtworkAI] CAPTRACK_LANGUAGE = " + language + "; PNG = " +
            result.artworkImagePath + "; Bytes = " + loadedArtworkPng.Length);
        yield return operation;
        UnityWebRequest request = finalAnalysisRequest;
        finalAnalysisRequest = null;
        try
        {
            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception("HTTP " + request.responseCode + ": " + request.error + "; " + request.downloadHandler.text);
            FinalArtworkResponse response = JsonUtility.FromJson<FinalArtworkResponse>(request.downloadHandler.text);
            if (response == null || response.language != language || string.IsNullOrWhiteSpace(response.finalArtworkFeedback))
                throw new Exception("The final artwork response is empty or has the wrong language.");
            CompleteFinalAnalysis(response.finalArtworkFeedback, language, null);
        }
        catch (Exception exception)
        {
            CompleteFinalAnalysis(null, language, exception.Message);
        }
        finally { request.Dispose(); }
        finalAnalysis = null;
    }

    private void CompleteFinalAnalysis(string feedback, string language, string error)
    {
        if (language != LanguageManager.CurrentLanguage) return;
        if (error != null) Debug.LogError("[FinalArtworkAI] Analysis failed: " + error);
        result.finalArtworkFeedback = !string.IsNullOrWhiteSpace(feedback) ? feedback :
            (language == "zh-TW" ? "你的作品完成了，謝謝你和我們一起創作。" :
                "Your artwork is complete. Thank you for creating with us.");
        result.finalFeedbackLanguage = language;
        // Keep album serialization compatible, using only the new final critique.
        result.aiDescription = result.finalArtworkFeedback;
        result.feedbackLanguage = language;
        analyzingFinalArtwork = false;
        DisplayAIFeedback();
        Debug.Log("[FinalArtworkAI] Response = " + result.finalArtworkFeedback);
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
            loadedArtworkPng = imageBytes; // Send the exact bytes decoded by ArtworkPreview, without recapture/re-encoding.
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
