using UnityEngine;

public class ArtworkResultManager : MonoBehaviour
{
    public static ArtworkResultManager Instance { get; private set; }

    public ArtworkResultData CurrentResult { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetResult(
        ArtworkData artworkData,
        string artworkImagePath,
        string theme)
    {
        CurrentResult = new ArtworkResultData();

        // Transfer a value snapshot, independent of the drawing scene's tracker.
        CurrentResult.artworkData = JsonUtility.FromJson<ArtworkData>(
            JsonUtility.ToJson(artworkData));
        CurrentResult.artworkImagePath = artworkImagePath;
        CurrentResult.theme = theme;
        AIFeedbackClient feedback = AIFeedbackClient.Instance;
        if (feedback != null && feedback.LatestFeedbackLanguage == LanguageManager.CurrentLanguage &&
            feedback.LatestFeedbackTheme == theme && !string.IsNullOrWhiteSpace(feedback.LatestFeedback))
        {
            CurrentResult.aiDescription = feedback.LatestFeedback;
            CurrentResult.feedbackLanguage = feedback.LatestFeedbackLanguage;
        }
        CurrentResult.createdAt =
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        Debug.Log("Artwork result data stored.");
        Debug.Log("[ResultManager] Language = " + LanguageManager.CurrentLanguage +
            "; Stored feedback = " + CurrentResult.aiDescription);
    }

    public bool HasResult()
    {
        return CurrentResult != null;
    }

    public void ClearResult()
    {
        CurrentResult = null;
    }

    public void RestoreResult(ArtworkResultData result)
    {
        CurrentResult = result;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
