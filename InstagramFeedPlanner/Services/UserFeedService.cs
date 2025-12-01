using InstagramFeedPlanner.Models;

namespace InstagramFeedPlanner.Services;

public class UserFeedService(FeedAndPostDbService dbService, IndexedDbImageService imageService)
{
    public List<Feed> Feeds { get; private set; } = null!;

    public Feed SelectedFeed { get; private set; } = null!;

    public async Task Initialize()
    {
        if (Feeds != null)
        {
            return;
        }

        var feeds = await dbService.GetAllFeedsAsync();

        feeds = feeds.OrderBy(i => i.CreatedDate).ToList();

        if (feeds == null || feeds.Count == 0)
        {
            Feeds = [];
            await AddNewFeed();
            return;
        }

        Feeds = feeds;

        var selectedFeed = feeds.First();

        selectedFeed.Posts ??= await GetPostsWithUpdatedBlobs(selectedFeed.Id);

        SelectedFeed = selectedFeed;
    }

    public async Task AddNewFeed()
    {
        var guid = Guid.NewGuid();

        var newFeed = new Feed(guid, $"Feed - {Feeds.Count + 1}")
        {
            Posts = []
        };

        Feeds.Add(newFeed);
        _ = dbService.AddFeedAsync(newFeed);

        SelectedFeed = newFeed;
    }

    public async Task SelectFeed(Guid feedId)
    {
        var newSelectedFeed = Feeds?.FirstOrDefault(e => e.Id == feedId);

        if (newSelectedFeed == null)
        {
            return;
        }

        newSelectedFeed.Posts ??= await GetPostsWithUpdatedBlobs(newSelectedFeed.Id);

        SelectedFeed = newSelectedFeed;
    }

    public void AddEmptyPost()
    {
        var position = SelectedFeed.Posts.Count == 0 ? 1 : SelectedFeed.Posts.Max(e => e.Position) + 1;

        var newPost = new Post(Guid.NewGuid(), SelectedFeed.Id);
        newPost.UpdatePosition(position);

        SelectedFeed.Posts.Add(newPost);
        _ = dbService.AddPostAsync(newPost);
    }

    public void DeletePost(Guid guid)
    {
        var requiredPost = SelectedFeed.Posts.FirstOrDefault(e => e.Id == guid);

        if (requiredPost == null)
        {
            return;
        }

        var postsToUpdate = SelectedFeed.Posts.Where(e => e.Position > requiredPost.Position).ToList();

        foreach (var post in postsToUpdate)
        {
            post.UpdatePosition(post.Position - 1);
        }

        _ = dbService.DeletePostAsync(requiredPost.Id);
        SelectedFeed.Posts.Remove(requiredPost);

        if (postsToUpdate.Count != 0)
        {
            _ = dbService.UpdateBatchPostsAsync(postsToUpdate);
        }
    }

    public void SwapPosts(Guid postId1, Guid postId2)
    {
        var post1 = SelectedFeed.Posts?.FirstOrDefault(e => e.Id == postId1);
        var post2 = SelectedFeed.Posts?.FirstOrDefault(e => e.Id == postId2);

        if (post1 == null || post2 == null)
        {
            return;
        }

        if (post1.IsLocked || post2.IsLocked)
        {
            return;
        }

        var position1 = post1.Position;
        var position2 = post2.Position;

        post1.UpdatePosition(position2);
        post2.UpdatePosition(position1);

        _ = dbService.UpdateBatchPostsAsync([post1, post2]);
    }

    public void InsertPostIntoPosition(Guid targetPostId, Guid targetPositionId)
    {
        var targetPost = SelectedFeed.Posts?.FirstOrDefault(e => e.Id == targetPostId);

        var targetPositionPost = SelectedFeed.Posts?.FirstOrDefault(e => e.Id == targetPositionId);

        if (targetPost == null || targetPositionPost == null)
        {
            return;
        }

        if (targetPost.IsLocked || targetPositionPost.IsLocked)
        {
            return;
        }

        var targetPosition = targetPositionPost.Position;

        List<Post> postsToUpdate;
        if (targetPost.Position < targetPosition)
        {
            postsToUpdate = SelectedFeed.Posts.Where(i => i.Position > targetPost.Position && i.Position <= targetPosition).ToList();

            foreach (var post in postsToUpdate)
            {
                post.UpdatePosition(post.Position - 1);
            }
        }
        else
        {
            postsToUpdate = SelectedFeed.Posts.Where(i => i.Position < targetPost.Position && i.Position >= targetPosition).ToList();

            foreach (var post in postsToUpdate)
            {
                post.UpdatePosition(post.Position + 1);
            }
        }

        targetPost.UpdatePosition(targetPosition);
        postsToUpdate.Add(targetPost);

        _ = dbService.UpdateBatchPostsAsync(postsToUpdate);
    }

    public void InitializeImage(Guid id, string blobKey, string url)
    {
        var post = SelectedFeed.Posts.FirstOrDefault(e => e.Id == id);

        if (post == null)
        {
            return;
        }

        post.UpdateUrl(blobKey, url, true);

        _ = dbService.UpdatePostAsync(post);
    }

    public void UpdateCropDetails(Guid id, CropData cropData)
    {
        var post = SelectedFeed.Posts?.FirstOrDefault(e => e.Id == id);

        if (post == null)
        {
            return;
        }

        post.UpdateCropData(cropData);
        _ = dbService.UpdatePostAsync(post);
    }

    public void UpdateLockStatus(Guid id)
    {
        var post = SelectedFeed.Posts?.FirstOrDefault(e => e.Id == id);

        if (post == null)
        {
            return;
        }

        post.ToggleLock();
        _ = dbService.UpdatePostAsync(post);
    }

    private async Task<List<Post>> GetPostsWithUpdatedBlobs(Guid feedId)
    {
        var posts = await dbService.GetPostsByFeedAsync(feedId);

        foreach (var post in posts)
        {
            if (string.IsNullOrEmpty(post.BlobKey))
            {
                continue;
            }

            var blobUrl = await imageService.GetBlobUrlAsync(post.BlobKey);

            if (blobUrl == null)
            {
                continue;
            }

            post.UpdateUrl(post.BlobKey, blobUrl);
        }

        return posts;
    }

    public async Task DeleteFeed(Guid id)
    {
        var feed = Feeds.FirstOrDefault(i => i.Id == id);

        if (feed == null)
        {
            return;
        }

        Feeds.Remove(feed);

        if (SelectedFeed.Id == id)
        {
            var newSelectedFeed = Feeds.FirstOrDefault();

            if (newSelectedFeed == null)
            {
                await AddNewFeed();
            }
            else
            {
                SelectedFeed = newSelectedFeed;
            }
        }

        await dbService.DeleteFeedAsync(feed.Id);
    }

    public async Task RenameFeed(Guid id, string name)
    {
        var feed = Feeds.FirstOrDefault(i => i.Id == id);

        if (feed == null)
        {
            return;
        }

        if (feed.Name.Equals(name))
        {
            return;
        }

        feed.Rename(name);

        await dbService.UpdateFeedAsync(feed);
    }
}