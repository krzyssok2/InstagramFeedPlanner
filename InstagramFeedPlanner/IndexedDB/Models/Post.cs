using Magic.IndexedDb;
using Magic.IndexedDb.SchemaAnnotations;
using static InstagramFeedPlanner.IndexedDB.Models.Post;

namespace InstagramFeedPlanner.IndexedDB.Models;

public class Post : MagicTableTool<Post>, IMagicTable<DbSets>
{
    [MagicNotMapped]
    public DbSets Databases { get; } = new();

    public sealed class DbSets
    {
        public readonly IndexedDbSet FeedPlanner = IndexDbContext.FeedPlanner;
    }

    public IndexedDbSet GetDefaultDatabase() => IndexDbContext.FeedPlanner;

    public List<IMagicCompoundIndex> GetCompoundIndexes() => new();

    public IMagicCompoundKey GetKeys() => CreatePrimaryKey(x => x.Id, false);

    public string GetTableName() => nameof(Post);

    [MagicName("id")]
    public Guid Id { get; set; } = new Guid();

    [MagicName("position")]
    public uint Position { get; set; }

    [MagicName("url")]
    public string? Url { get; set; }

    [MagicName("cropData")]
    public CropData CropData { get; set; } = new CropData();

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

public class CropData
{
    [MagicName("posX")]
    public double PosX { get; set; }

    [MagicName("posY")]
    public double PosY { get; set; }

    [MagicName("scale")]
    public double Scale { get; set; }

    [MagicName("zoomValue")]
    public double ZoomValue { get; set; }
}