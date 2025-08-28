using InstagramFeedPlanner.Extensions;
using InstagramFeedPlanner.Models;
using InstagramFeedPlanner.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace InstagramFeedPlanner.Pages;

public partial class Planner(IJSRuntime js, UserFeedService FeedService, IndexedDbImageService indexedDbImageService)
{
    private DotNetObjectReference<Planner>? objRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            objRef = DotNetObjectReference.Create(this);
            await js.InvokeVoidAsync("visibilityHandler.register", objRef);

            await FeedService.Initialize();

            StateHasChanged();
        }
    }

    private async Task RenameFeed((Guid feedId, string newName) renameInfo)
    {
        await FeedService.RenameFeed(renameInfo.feedId, renameInfo.newName);
        StateHasChanged();
    }

    private async Task SelectFeed(Guid id)
    {
        await FeedService.SelectFeed(id);
        StateHasChanged();
    }

    private async Task DeleteFeed(Guid id)
    {
        await FeedService.DeleteFeed(id);
        StateHasChanged();
    }

    private Guid? draggedItemId;
    private Post? adjustingElement;

    private void AddEmptyPost() => FeedService.AddEmptyPost();

    private async Task AddNewFeed()
    {
        await FeedService.AddNewFeed();
        StateHasChanged();
    }

    private void OnPostDelete(Guid id) => FeedService.DeletePost(id);

    private void OnAdjust(Post element) => adjustingElement = element;

    private void OnLock(Guid id) => FeedService.UpdateLockStatus(id);

    private void CancelAdjust() => adjustingElement = null;

    private void OnDragStart(Guid id) => draggedItemId = id;

    private async Task OnDrop((Guid PostId, DragEventArgs Args) result)
    {
        if (draggedItemId != null)
        {
            HandlePostDrop(draggedItemId.Value, result.PostId, result.Args);

            draggedItemId = null;
            StateHasChanged();
            return;
        }

        // Image drop logic
        var imageUrl = await js.InvokeAsync<string>("dragDropHelper.getImageFromDropEvent");
        await HandleImage(result.PostId, imageUrl);
    }

    private async Task OnImageUpload((Guid PostId, string Url) result) => await HandleImage(result.PostId, result.Url);

    private async Task HandleImage(Guid postId, string imageUrl)
    {
        if (!string.IsNullOrEmpty(imageUrl))
        {
            var blobKey = await indexedDbImageService.SaveImageAsync(imageUrl);

            var blobUrl = await indexedDbImageService.GetBlobUrlAsync(blobKey);

            if (blobUrl != null)
            {
                FeedService.InitializeImage(postId, blobKey, blobUrl);
                return;
            }
            StateHasChanged();
        }
    }

    private void HandlePostDrop(Guid draggedPost, Guid targetPost, DragEventArgs e)
    {
        if (e.ShiftKey)
        {
            FeedService.SwapPosts(draggedPost, targetPost);
        }
        else
        {
            FeedService.InsertPostIntoPosition(draggedPost, targetPost);
        }
    }

    private void OnCropConfirmed(CropDataModel result)
    {
        if (adjustingElement != null)
        {
            FeedService.UpdateCropDetails(adjustingElement!.Id, result.ToCropData());

            adjustingElement = null;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task OnVisibilityChanged(bool isVisible)
    {
        if (isVisible)
        {
            //TO DO:
            //await FeedService.Initialize();
            StateHasChanged();
        }
    }
}