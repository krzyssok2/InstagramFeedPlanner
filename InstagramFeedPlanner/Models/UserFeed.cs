namespace InstagramFeedPlanner.Models;

public class UserFeed
{
    public List<InstagramElement> Posts { get; private set; } = [];

    public UserFeed(List<InstagramElement>? posts)
    {
        if (posts == null)
        {
            Posts = [];
        }
        else
        {
            Posts = posts;
        }
    }

    public void AddEmptyPost()
    {
        var position = Posts.Count == 0 ? 1 : Posts.Max(e => e.Position) + 1;

        Posts.Add(new InstagramElement(position));
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

        Posts.Remove(requiredPost);
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

        if (targetPost.Position < targetPosition)
        {
            var postToUpdate = Posts.Where(i => i.Position > targetPost.Position && i.Position <= targetPosition);

            foreach (var post in postToUpdate)
            {
                post.UpdatePosition(post.Position - 1);
            }
        }
        else
        {
            var postToUpdate = Posts.Where(i => i.Position < targetPost.Position && i.Position >= targetPosition);

            foreach (var post in postToUpdate)
            {
                post.UpdatePosition(post.Position + 1);
            }
        }

        targetPost.UpdatePosition(targetPosition);
    }

    public void InitializeImage(Guid id, string url)
    {
        var post = Posts.FirstOrDefault(e => e.Id == id);

        if (post == null)
        {
            return;
        }

        post.UpdateUrl(url);
    }
}