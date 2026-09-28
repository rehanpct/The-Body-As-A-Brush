using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ArtworkAlbumManager : MonoBehaviour
{
    public static ArtworkAlbumManager Instance { get; private set; }

    private const string AlbumFileName = "CAPTRACK_Album.json";

    private List<AlbumArtworkData> artworks =
        new List<AlbumArtworkData>();

    private string AlbumFilePath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                AlbumFileName
            );
        }
    }

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        LoadAlbum();
    }

    // =========================================================
    // SAVE ARTWORK
    // =========================================================

    public bool SaveArtwork(ArtworkResultData result)
    {
        if (result == null)
        {
            Debug.LogError(
                "ArtworkAlbumManager: Result data is NULL."
            );

            return false;
        }

        if (result.artworkData == null)
        {
            Debug.LogError(
                "ArtworkAlbumManager: ArtworkData is NULL."
            );

            return false;
        }

        if (string.IsNullOrEmpty(result.artworkImagePath))
        {
            Debug.LogError(
                "ArtworkAlbumManager: Artwork image path is empty."
            );

            return false;
        }

        if (!File.Exists(result.artworkImagePath))
        {
            Debug.LogError(
                "ArtworkAlbumManager: Artwork image does not exist:\n" +
                result.artworkImagePath
            );

            return false;
        }

        // -----------------------------------------------------
        // CREATE ALBUM ENTRY
        // -----------------------------------------------------

        AlbumArtworkData albumArtwork =
            new AlbumArtworkData();

        albumArtwork.id =
            System.Guid.NewGuid().ToString();

        albumArtwork.artworkImagePath =
            result.artworkImagePath;

        albumArtwork.theme =
            result.theme;

        albumArtwork.createdAt =
            result.createdAt;

        albumArtwork.artworkData =
            result.artworkData;

        albumArtwork.aiTitle =
            result.artworkTitle;

        albumArtwork.aiDescription =
            result.aiDescription;

        albumArtwork.aiInterpretation =
            result.aiInterpretation;

        // -----------------------------------------------------
        // ADD TO ALBUM
        // -----------------------------------------------------

        artworks.Add(albumArtwork);

        // -----------------------------------------------------
        // WRITE JSON
        // -----------------------------------------------------

        SaveAlbum();

        Debug.Log("================================");
        Debug.Log("ARTWORK SAVED TO ALBUM");
        Debug.Log("================================");

        Debug.Log(
            "Album ID: " +
            albumArtwork.id
        );

        Debug.Log(
            "Theme: " +
            albumArtwork.theme
        );

        Debug.Log(
            "Created At: " +
            albumArtwork.createdAt
        );

        Debug.Log(
            "Artwork Path: " +
            albumArtwork.artworkImagePath
        );

        Debug.Log(
            "Total Album Artworks: " +
            artworks.Count
        );

        Debug.Log("================================");

        return true;
    }

    // =========================================================
    // SAVE ALBUM TO JSON
    // =========================================================

    private void SaveAlbum()
    {
        AlbumContainer container =
            new AlbumContainer();

        container.artworks =
            artworks.ToArray();

        string json =
            JsonUtility.ToJson(
                container,
                true
            );

        try
        {
            File.WriteAllText(
                AlbumFilePath,
                json
            );

            Debug.Log(
                "CAPTRACK album saved to:\n" +
                AlbumFilePath
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "ArtworkAlbumManager: " +
                "Failed to save album.\n" +
                exception.Message
            );
        }
    }

    // =========================================================
    // LOAD ALBUM
    // =========================================================

    private void LoadAlbum()
    {
        artworks.Clear();

        if (!File.Exists(AlbumFilePath))
        {
            Debug.Log(
                "No existing CAPTRACK album found."
            );

            return;
        }

        try
        {
            string json =
                File.ReadAllText(
                    AlbumFilePath
                );

            AlbumContainer container =
                JsonUtility.FromJson<AlbumContainer>(
                    json
                );

            if (container != null &&
                container.artworks != null)
            {
                artworks =
                    new List<AlbumArtworkData>(
                        container.artworks
                    );
            }

            Debug.Log(
                "CAPTRACK album loaded. " +
                "Artworks: " +
                artworks.Count
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "ArtworkAlbumManager: " +
                "Failed to load album.\n" +
                exception.Message
            );

            artworks =
                new List<AlbumArtworkData>();
        }
    }

    // =========================================================
    // GET ALL ARTWORKS
    // =========================================================

    public List<AlbumArtworkData> GetAllArtworks()
    {
        return new List<AlbumArtworkData>(
            artworks
        );
    }

    // =========================================================
    // GET ARTWORK BY ID
    // =========================================================

    public AlbumArtworkData GetArtworkById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        foreach (AlbumArtworkData artwork in artworks)
        {
            if (artwork.id == id)
            {
                return artwork;
            }
        }

        return null;
    }

    // =========================================================
    // DELETE ARTWORK
    // =========================================================

    public bool DeleteArtwork(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning(
                "ArtworkAlbumManager: Cannot delete empty ID."
            );

            return false;
        }

        AlbumArtworkData artwork =
            GetArtworkById(id);

        if (artwork == null)
        {
            Debug.LogWarning(
                "ArtworkAlbumManager: Artwork not found: " +
                id
            );

            return false;
        }

        // -----------------------------------------------------
        // REMOVE PNG FILE
        // -----------------------------------------------------

        if (!string.IsNullOrEmpty(
            artwork.artworkImagePath))
        {
            try
            {
                if (File.Exists(
                    artwork.artworkImagePath))
                {
                    File.Delete(
                        artwork.artworkImagePath
                    );

                    Debug.Log(
                        "Artwork PNG deleted."
                    );
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    "Could not delete artwork PNG:\n" +
                    exception.Message
                );
            }
        }

        // -----------------------------------------------------
        // REMOVE FROM LIST
        // -----------------------------------------------------

        artworks.Remove(artwork);

        SaveAlbum();

        Debug.Log(
            "Artwork removed from album: " +
            id
        );

        return true;
    }

    // =========================================================
    // ALBUM COUNT
    // =========================================================

    public int GetArtworkCount()
    {
        return artworks.Count;
    }

    // =========================================================
    // CHECK WHETHER ARTWORK EXISTS
    // =========================================================

    public bool ContainsArtwork(string id)
    {
        return GetArtworkById(id) != null;
    }

    // =========================================================
    // CLEAR ENTIRE ALBUM
    // =========================================================

    public void ClearAlbum()
    {
        foreach (AlbumArtworkData artwork in artworks)
        {
            if (artwork == null)
                continue;

            if (string.IsNullOrEmpty(
                artwork.artworkImagePath))
                continue;

            try
            {
                if (File.Exists(
                    artwork.artworkImagePath))
                {
                    File.Delete(
                        artwork.artworkImagePath
                    );
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    "Could not delete artwork file:\n" +
                    exception.Message
                );
            }
        }

        artworks.Clear();

        SaveAlbum();

        Debug.Log(
            "CAPTRACK album cleared."
        );
    }

    // =========================================================
    // ALBUM FILE LOCATION
    // =========================================================

    public string GetAlbumFilePath()
    {
        return AlbumFilePath;
    }

    // =========================================================
    // JSON CONTAINER
    // =========================================================

    [System.Serializable]
    private class AlbumContainer
    {
        public AlbumArtworkData[] artworks;
    }
}