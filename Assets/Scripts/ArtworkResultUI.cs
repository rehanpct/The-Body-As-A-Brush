using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ArtworkResultUI : MonoBehaviour
{
    [Header("UI References")]
    public RawImage artworkPreview;

    public Text themeText;
    public Text creationTimeText;

    public Text waterText;
    public Text coralText;
    public Text fishText;
    public Text bubblesText;
    public Text lanternText;

    public Text indexText;
    public Text openPalmText;
    public Text vSignText;
    public Text thumbUpText;

    public Text totalDistanceText;
    public Text averageSpeedText;
    public Text maximumSpeedText;
    public Text pauseTimeText;

    public Text handSamplesText;
    public Text gesturesUsedText;
    public Text totalElementsText;

    public Text aiTitleText;
    public Text aiDescriptionText;

    private ArtworkResultData result;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        LoadResultData();
    }

    // =========================================================
    // LOAD RESULT DATA
    // =========================================================

    private void LoadResultData()
    {
        if (ArtworkResultManager.Instance == null)
        {
            Debug.LogError(
                "ArtworkResultUI: " +
                "ArtworkResultManager.Instance is NULL."
            );

            return;
        }

        if (!ArtworkResultManager.Instance.HasResult())
        {
            Debug.LogError(
                "ArtworkResultUI: " +
                "No artwork result data available."
            );

            return;
        }

        result =
            ArtworkResultManager.Instance.CurrentResult;

        if (result.artworkData == null)
        {
            Debug.LogError(
                "ArtworkResultUI: ArtworkData is NULL."
            );

            return;
        }

        DisplayResult();
    }

    // =========================================================
    // DISPLAY RESULT
    // =========================================================

    private void DisplayResult()
    {
        ArtworkData data =
            result.artworkData;

        // -----------------------------------------------------
        // GENERAL
        // -----------------------------------------------------

        SetText(
            themeText,
            "Theme: " + result.theme
        );

        SetText(
            creationTimeText,
            "Creation Time: " +
            FormatTime(data.creationTime)
        );

        // -----------------------------------------------------
        // ELEMENTS
        // -----------------------------------------------------

        SetText(
            waterText,
            "Water Currents: " +
            data.waterCount
        );

        SetText(
            coralText,
            "Coral: " +
            data.coralCount
        );

        SetText(
            fishText,
            "Fish Schools: " +
            data.fishSchoolCount
        );

        SetText(
            bubblesText,
            "Bubble Bursts: " +
            data.bubbleBurstCount
        );

        SetText(
            lanternText,
            "Lanterns: " +
            data.lanternCount
        );

        // -----------------------------------------------------
        // GESTURES
        // -----------------------------------------------------

        SetText(
            indexText,
            "Index: " +
            data.indexGestureCount
        );

        SetText(
            openPalmText,
            "Open Palm: " +
            data.openPalmGestureCount
        );

        SetText(
            vSignText,
            "V Sign: " +
            data.vSignGestureCount
        );

        SetText(
            thumbUpText,
            "Thumb Up: " +
            data.thumbUpGestureCount
        );

        // -----------------------------------------------------
        // HAND MOVEMENT
        // -----------------------------------------------------

        SetText(
            totalDistanceText,
            "Total Distance: " +
            data.totalHandDistance.ToString("F2")
        );

        SetText(
            averageSpeedText,
            "Average Speed: " +
            data.averageHandSpeed.ToString("F2")
        );

        SetText(
            maximumSpeedText,
            "Maximum Speed: " +
            data.maximumHandSpeed.ToString("F2")
        );

        SetText(
            pauseTimeText,
            "Pause Time: " +
            data.pauseTime.ToString("F1") +
            " s"
        );

        // -----------------------------------------------------
        // JOURNEY
        // -----------------------------------------------------

        SetText(
            handSamplesText,
            "Hand Tracking Samples: " +
            data.handTrackingSamples
        );

        int gesturesUsed =
            data.indexGestureCount +
            data.openPalmGestureCount +
            data.vSignGestureCount +
            data.thumbUpGestureCount;

        SetText(
            gesturesUsedText,
            "Gestures Used: " +
            gesturesUsed
        );

        int totalElements =
            data.waterCount +
            data.coralCount +
            data.fishSchoolCount +
            data.bubbleBurstCount +
            data.lanternCount;

        SetText(
            totalElementsText,
            "Total Elements: " +
            totalElements
        );

        // -----------------------------------------------------
        // ARTWORK
        // -----------------------------------------------------

        LoadArtworkImage();

        // -----------------------------------------------------
        // TEMPORARY AI
        // -----------------------------------------------------

        SetText(
            aiTitleText,
            string.IsNullOrEmpty(
                result.artworkTitle)
                ? "Your Creation"
                : result.artworkTitle
        );

        SetText(
            aiDescriptionText,
            string.IsNullOrEmpty(
                result.aiDescription)
                ? "AI interpretation will appear here."
                : result.aiDescription
        );

        Debug.Log("================================");
        Debug.Log("RESULT DATA DISPLAYED");
        Debug.Log("Theme: " + result.theme);
        Debug.Log("Artwork: " + result.artworkImagePath);
        Debug.Log("================================");
    }

    // =========================================================
    // LOAD ARTWORK PNG
    // =========================================================

    private void LoadArtworkImage()
    {
        if (artworkPreview == null)
        {
            Debug.LogWarning(
                "ArtworkResultUI: " +
                "Artwork Preview is not assigned."
            );

            return;
        }

        if (string.IsNullOrEmpty(
            result.artworkImagePath))
        {
            Debug.LogError(
                "ArtworkResultUI: " +
                "Artwork image path is empty."
            );

            return;
        }

        if (!File.Exists(
            result.artworkImagePath))
        {
            Debug.LogError(
                "ArtworkResultUI: " +
                "Artwork file does not exist:\n" +
                result.artworkImagePath
            );

            return;
        }

        byte[] imageBytes =
            File.ReadAllBytes(
                result.artworkImagePath
            );

        Texture2D texture =
            new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false
            );

        bool loaded =
            texture.LoadImage(
                imageBytes
            );

        if (!loaded)
        {
            Debug.LogError(
                "ArtworkResultUI: " +
                "Failed to load artwork PNG."
            );

            Destroy(texture);

            return;
        }

        artworkPreview.texture =
            texture;

        Debug.Log(
            "Artwork preview loaded successfully."
        );
    }

    // =========================================================
    // SAVE TO ALBUM
    // =========================================================

    public void SaveToAlbum()
    {
        if (result == null)
        {
            Debug.LogError(
                "ArtworkResultUI: " +
                "No result to save."
            );

            return;
        }

        if (ArtworkAlbumManager.Instance == null)
        {
            Debug.LogError(
                "ArtworkResultUI: " +
                "ArtworkAlbumManager.Instance is NULL."
            );

            return;
        }

        bool saved =
            ArtworkAlbumManager.Instance.SaveArtwork(
                result
            );

        if (saved)
        {
            Debug.Log(
                "ArtworkResultUI: " +
                "Artwork successfully added to album."
            );
        }
        else
        {
            Debug.LogError(
                "ArtworkResultUI: " +
                "Failed to save artwork to album."
            );
        }
    }

    // =========================================================
    // CREATE AGAIN
    // =========================================================

    public void CreateAgain()
    {
        Debug.Log(
            "CREATE AGAIN pressed."
        );

        if (ArtworkResultManager.Instance != null)
        {
            ArtworkResultManager.Instance.ClearResult();
        }

        SceneManager.LoadScene(
            "DrawingScene"
        );
    }

    // =========================================================
    // BACK TO MENU
    // =========================================================

    public void BackToMenu()
    {
        Debug.Log(
            "BACK TO MENU pressed."
        );

        if (ArtworkResultManager.Instance != null)
        {
            ArtworkResultManager.Instance.ClearResult();
        }

        SceneManager.LoadScene(
            "MainMenu"
        );
    }

    // =========================================================
    // TEXT HELPER
    // =========================================================

    private void SetText(
        Text textComponent,
        string value)
    {
        if (textComponent != null)
        {
            textComponent.text = value;
        }
    }

    // =========================================================
    // TIME FORMAT
    // =========================================================

    private string FormatTime(
        float seconds)
    {
        int minutes =
            Mathf.FloorToInt(
                seconds / 60f
            );

        int remainingSeconds =
            Mathf.FloorToInt(
                seconds % 60f
            );

        return
            minutes.ToString("00") +
            ":" +
            remainingSeconds.ToString("00");
    }
}