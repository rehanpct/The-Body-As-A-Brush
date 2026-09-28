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

        CurrentResult.artworkData = artworkData;
        CurrentResult.artworkImagePath = artworkImagePath;
        CurrentResult.theme = theme;
        CurrentResult.createdAt =
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        Debug.Log("Artwork result data stored.");
    }

    public bool HasResult()
    {
        return CurrentResult != null;
    }

    public void ClearResult()
    {
        CurrentResult = null;
    }
}