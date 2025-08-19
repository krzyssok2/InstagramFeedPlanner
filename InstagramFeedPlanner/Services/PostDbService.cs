namespace InstagramFeedPlanner.Services;

using InstagramFeedPlanner.Models;
using Microsoft.JSInterop;

public class PostDbService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private const string DbName = "MyPostsDb";
    private const int DbVersion = 1;
    private const string StoreName = "posts";

    public PostDbService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitAsync()
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./postDbService.js");

        await _module.InvokeVoidAsync("initDb", DbName, DbVersion, StoreName);
    }

    public async Task AddPostAsync(Post post)
    {
        await EnsureModule();
        await _module!.InvokeVoidAsync("addPost", StoreName, post);
    }

    public async Task DeletePostAsync(Guid id)
    {
        await EnsureModule();
        await _module!.InvokeVoidAsync("deletePost", StoreName, id.ToString());
    }

    public async Task UpdatePostAsync(Post post) => await UpdateBatchPostsAsync([post]);

    public async Task UpdateBatchPostsAsync(IEnumerable<Post> posts)
    {
        await EnsureModule();
        await _module!.InvokeVoidAsync("updateBatchPosts", StoreName, posts);
    }

    public async Task<Post?> GetPostAsync(Guid id)
    {
        await EnsureModule();
        return await _module!.InvokeAsync<Post?>("getPost", StoreName, id.ToString());
    }

    public async Task<List<Post>> GetAllPostsAsync()
    {
        await EnsureModule();
        return await _module!.InvokeAsync<List<Post>>("getAllPosts", StoreName);
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