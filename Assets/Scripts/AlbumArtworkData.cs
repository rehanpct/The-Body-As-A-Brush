using System;

[Serializable]
public class AlbumArtworkData
{
    public string id;
    public string artworkImagePath;
    public string theme;
    public string createdAt;

    public ArtworkData artworkData;

    public string aiTitle;
    public string aiDescription;
    public string aiInterpretation;

    public AlbumArtworkData()
    {
        id = "";
        artworkImagePath = "";
        theme = "";
        createdAt = "";

        artworkData = new ArtworkData();

        aiTitle = "";
        aiDescription = "";
        aiInterpretation = "";
    }
}