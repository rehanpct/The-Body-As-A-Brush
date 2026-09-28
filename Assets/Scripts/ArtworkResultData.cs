using System;

[Serializable]
public class ArtworkResultData
{
    public ArtworkData artworkData;

    public string artworkImagePath;
    public string artworkTitle;
    public string aiDescription;
    public string aiInterpretation;

    public string theme;
    public string createdAt;

    public ArtworkResultData()
    {
        artworkData = new ArtworkData();
        artworkImagePath = "";
        artworkTitle = "";
        aiDescription = "";
        aiInterpretation = "";
        theme = "";
        createdAt = "";
    }
}