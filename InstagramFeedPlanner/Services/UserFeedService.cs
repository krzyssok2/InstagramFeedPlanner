using InstagramFeedPlanner.Models;

namespace InstagramFeedPlanner.Services;

public class UserFeedService(PostDbService dbService, IndexedDbImageService indexedDbImageService)
{
    public List<Post> Posts { get; private set; } = null!;

    private bool IsInitialized;

    public async Task Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        var posts = await dbService.GetAllPostsAsync();

        foreach (var post in posts)
        {
            if (string.IsNullOrEmpty(post.BlobKey))
            {
                continue;
            }

            var blobUrl = await indexedDbImageService.GetBlobUrlAsync(post.BlobKey);

            if (blobUrl == null)
            {
                continue;
            }

            post.UpdateUrl(post.BlobKey, blobUrl);
        }

        Posts = posts;
        IsInitialized = true;
    }

    public void AddEmptyPost()
    {
        var position = Posts.Count == 0 ? 1 : Posts.Max(e => e.Position) + 1;

        var newPost = new Post() { Id = Guid.NewGuid() };
        newPost.UpdatePosition(position);

        Posts.Add(newPost);
        _ = dbService.AddPostAsync(newPost);
    }

    public void DeletePost(Guid guid)
    {
        var requiredPost = Posts.FirstOrDefault(e => e.Id == guid);

        if (requiredPost == null)
        {
            return;
        }

        var postsToUpdate = Posts.Where(e => e.Position > requiredPost.Position).ToList();

        foreach (var post in postsToUpdate)
        {
            post.UpdatePosition(post.Position - 1);
        }

        _ = dbService.DeletePostAsync(requiredPost.Id);
        Posts.Remove(requiredPost);

        if (postsToUpdate.Count != 0)
        {
            _ = dbService.UpdateBatchPostsAsync(postsToUpdate);
        }
    }

    public void SwapPosts(Guid postId1, Guid postId2)
    {
        var post1 = Posts.FirstOrDefault(e => e.Id == postId1);
        var post2 = Posts.FirstOrDefault(e => e.Id == postId2);

        if (post1 == null || post2 == null)
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
        var targetPost = Posts.FirstOrDefault(e => e.Id == targetPostId);

        var targetPositionPost = Posts.FirstOrDefault(e => e.Id == targetPositionId);

        if (targetPost == null || targetPositionPost == null)
        {
            return;
        }

        var targetPosition = targetPositionPost.Position;

        List<Post> postsToUpdate;
        if (targetPost.Position < targetPosition)
        {
            postsToUpdate = Posts.Where(i => i.Position > targetPost.Position && i.Position <= targetPosition).ToList();

            foreach (var post in postsToUpdate)
            {
                post.UpdatePosition(post.Position - 1);
            }
        }
        else
        {
            postsToUpdate = Posts.Where(i => i.Position < targetPost.Position && i.Position >= targetPosition).ToList();

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
        var post = Posts.FirstOrDefault(e => e.Id == id);

        if (post == null)
        {
            return;
        }

        post.UpdateUrl(blobKey, url, true);

        _ = dbService.UpdatePostAsync(post);
    }

    public void UpdateCropDetails(Guid id, CropData cropData)
    {
        var post = Posts.FirstOrDefault(e => e.Id == id);

        if (post == null)
        {
            return;
        }

        post.UpdateCropData(cropData);
        _ = dbService.UpdatePostAsync(post);
    }
}