namespace InstagramFeedPlanner.Models;

public class InstagramElement(uint position)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public uint Position { get; private set; } = position;
    public string? Url { get; private set; }
    public CropData CropData { get; private set; } = new CropData();

    public void UpdatePosition(uint position) => Position = position;

    public void UpdateUrl(string url)
    {
        Url = url;
        CropData = new CropData();
    }

    public void UpdateCropData(CropData cropData)
    {
        CropData.PosX = cropData.PosX;
        CropData.PosY = cropData.PosY;
        CropData.Scale = cropData.Scale;
        CropData.ZoomValue = cropData.ZoomValue;
    }
}