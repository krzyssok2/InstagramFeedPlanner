using System.Text.Json.Serialization;

namespace InstagramFeedPlanner.Models;

public class Post
{
    public Guid Id { get; set; } = new Guid();

    public uint Position { get; set; }

    public string? BlobKey { get; set; }

    [JsonIgnore]
    public string? Url { get; set; }

    public CropData CropData { get; set; } = new CropData();

    public void UpdatePosition(uint position) => Position = position;

    public void UpdateUrl(string blobKey, string url, bool resetCropData = false)
    {
        BlobKey = blobKey;
        Url = url;

        if (resetCropData)
        {
            CropData = new CropData();
        }
    }

    public void UpdateCropData(CropData cropData)
    {
        CropData.PosX = cropData.PosX;
        CropData.PosY = cropData.PosY;
        CropData.Scale = cropData.Scale;
        CropData.ZoomValue = cropData.ZoomValue;
    }
}

public class CropData
{
    public double PosX { get; set; }

    public double PosY { get; set; }

    public double Scale { get; set; }

    public double ZoomValue { get; set; }
}