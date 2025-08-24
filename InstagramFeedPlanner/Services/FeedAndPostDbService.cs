namespace InstagramFeedPlanner.Services;

using InstagramFeedPlanner.Models;
using Microsoft.JSInterop;

public class FeedAndPostDbService(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    private const string DbName = "MyPostsDb";
    private const int DbVersion = 1;

    public async Task InitAsync()
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./postDbService.js");
        await _module.InvokeVoidAsync("initDb", DbName, DbVersion);
    }

    public async Task AddFeedAsync(Feed feed)
    {
        await EnsureModule();
        await _module!.InvokeVoidAsync("addFeed", feed);
    }

    public async Task UpdateFeedAsync(Feed feed)
    {
        await EnsureModule();
        await _module!.InvokeVoidAsync("updateFeed", feed);
    }

    public async Task DeleteFeedAsync(Guid id)
    {
        await EnsureModule();
        await _module!.InvokeVoidAsync("deleteFeed", id.ToString());
    }

    public async Task<Feed?> GetFeedAsync(Guid id)
    {
        await EnsureModule();
        return await _module!.InvokeAsync<Feed?>("getFeed", id.ToString());
    }

    public async Task<List<Feed>> GetAllFeedsAsync()
    {
        await EnsureModule();
        return await _module!.InvokeAsync<List<Feed>>("getAllFeeds");
    }

    public async Task AddPostAsync(Post post)
    {
        await EnsureModule();
        await _module!.InvokeVoidAsync("addPost", post);
    }

    public async Task DeletePostAsync(Guid id)
    {
        await EnsureModule();
        await _module!.InvokeVoidAsync("deletePost", id.ToString());
    }

    public async Task UpdatePostAsync(Post post) => await UpdateBatchPostsAsync([post]);

    public async Task UpdateBatchPostsAsync(IEnumerable<Post> posts)
    {
        await EnsureModule();
        await _module!.InvokeVoidAsync("updateBatchPosts", posts);
    }

    public async Task<Post?> GetPostAsync(Guid id)
    {
        await EnsureModule();
        return await _module!.InvokeAsync<Post?>("getPost", id.ToString());
    }

    public async Task<List<Post>> GetAllPostsAsync()
    {
        await EnsureModule();
        return await _module!.InvokeAsync<List<Post>>("getAllPosts");
    }

    public async Task<List<Post>> GetPostsByFeedAsync(Guid feedId)
    {
        await EnsureModule();
        return await _module!.InvokeAsync<List<Post>>("getPostsByFeed", feedId.ToString());
    }

    private async Task EnsureModule()
    {
        if (_module == null)
        {
            await InitAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}