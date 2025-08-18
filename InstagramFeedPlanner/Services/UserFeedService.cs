using InstagramFeedPlanner.IndexedDB.Models;
using Magic.IndexedDb;

namespace InstagramFeedPlanner.Services;

public class UserFeedService
{
    public List<Post> Posts { get; private set; } = [];

    private IMagicQuery<Post> PostQuery;

    public UserFeedService(IMagicQuery<Post> query)
    {
        PostQuery = query;
    }

    public async Task Initialize() => Posts = await PostQuery.ToListAsync();

    public async Task AddEmptyPost()
    {
        var position = Posts.Count == 0 ? 1 : Posts.Max(e => e.Position) + 1;

        var newPost = new Post() { Id = Guid.NewGuid() };
        newPost.UpdatePosition(position);

        Posts.Add(newPost);
        PostQuery.AddAsync(newPost);
    }

    public async Task DeletePost(Guid guid)
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

        await PostQuery.DeleteAsync(requiredPost);
        Posts.Remove(requiredPost);

        if (postsToUpdate.Count != 0)
        {
            await PostQuery.UpdateRangeAsync(postsToUpdate);
        }
    }

    public async Task SwapPosts(Guid postId1, Guid postId2)
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

        await PostQuery.UpdateRangeAsync([post1, post2]);
    }

    public async Task InsertPostIntoPosition(Guid targetPostId, Guid targetPositionId)
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
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Yield(); // let UI thread breathe
                await PostQuery.UpdateRangeAsync(postsToUpdate);
            }
            catch (Exception ex)
            {
                // log somewhere, otherwise exception is swallowed
                Console.Error.WriteLine($"Failed to update posts: {ex}");
            }
        });
    }

    public async Task InitializeImage(Guid id, string url)
    {
        var post = Posts.FirstOrDefault(e => e.Id == id);

        if (post == null)
        {
            return;
        }

        post.UpdateUrl(url);

        await PostQuery.UpdateAsync(post);
    }

    public async Task UpdateCropDetails(Guid id, CropData cropData)
    {
        var post = Posts.FirstOrDefault(e => e.Id == id);

        if (post == null)
        {
            return;
        }

        post.UpdateCropData(cropData);
        await PostQuery.UpdateAsync(post);
    }
}