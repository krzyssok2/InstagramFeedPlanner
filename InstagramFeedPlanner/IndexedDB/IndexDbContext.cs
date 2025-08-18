using Magic.IndexedDb;
using Magic.IndexedDb.Interfaces;

namespace InstagramFeedPlanner.IndexedDB;

public class IndexDbContext : IMagicRepository
{
    public static readonly IndexedDbSet FeedPlanner = new(DbNames.FeedPlanner);
}