using Microsoft.JSInterop;

namespace InstagramFeedPlanner.Services;

public class IndexedDbImageService : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public IndexedDbImageService(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./indexedDbImages.js").AsTask());
    }

    public async Task<string> SaveImageAsync(string imageUrl)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("saveImage", imageUrl);
    }

    public async Task<string?> GetBlobUrlAsync(string hash)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string?>("getBlobUrl", hash);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }

    public async Task<bool> DeleteImageAsync(string hash)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("deleteImage", hash);
    }

    public async Task<List<ImageEntry>> GetAllImagesAsync()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<List<ImageEntry>>("getAllImages");
    }

    public record ImageEntry(string Key, string Url);
}