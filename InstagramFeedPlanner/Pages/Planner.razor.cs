using InstagramFeedPlanner.Extensions;
using InstagramFeedPlanner.Models;
using InstagramFeedPlanner.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace InstagramFeedPlanner.Pages;

public partial class Planner(IJSRuntime js, UserFeedService FeedService, IndexedDbImageService indexedDbImageService)
{
    private Guid? SelectedFeedId = null;

    private DotNetObjectReference<Planner>? objRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            objRef = DotNetObjectReference.Create(this);
            await js.InvokeVoidAsync("visibilityHandler.register", objRef);

            await FeedService.Initialize();

            SelectedFeedId = FeedService.SelectedFeed.Id;

            StateHasChanged();
        }
    }

    private async Task OnFeedChanged()
    {
        await FeedService.SelectFeed(SelectedFeedId!.Value);
    }

    private async Task SelectFeed(Guid id)
    {
        SelectedFeedId = id;
        await FeedService.SelectFeed(id);
    }

    private async Task DeleteFeed(Guid id)
    {
        await FeedService.DeleteFeed(id);
        SelectedFeedId = FeedService.SelectedFeed?.Id;
        StateHasChanged();
    }

    private Guid? draggedItemId;
    private Post? adjustingElement;

    private void AddEmptyPost() => FeedService.AddEmptyPost();

    private async Task AddNewFeed()
    {
        await FeedService.AddNewFeed();
        SelectedFeedId = FeedService.SelectedFeed.Id;
    }

    private void OnPostDelete(Guid id) => FeedService.DeletePost(id);

    private void OnAdjust(Post element) => adjustingElement = element;

    private void OnLock(Guid id) => FeedService.UpdateLockStatus(id);

    private void CancelAdjust() => adjustingElement = null;

    private void OnDragStart(Guid id) => draggedItemId = id;

    private async Task OnDrop(Guid id, DragEventArgs e)
    {
        if (draggedItemId != null)
        {
            HandlePostDrop(draggedItemId.Value, id, e);

            draggedItemId = null;
            StateHasChanged();
            return;
        }

        // Image drop logic
        var imageUrl = await js.InvokeAsync<string>("dragDropHelper.getImageFromDropEvent");
        if (!string.IsNullOrEmpty(imageUrl))
        {
            var blobKey = await indexedDbImageService.SaveImageAsync(imageUrl);

            var blobUrl = await indexedDbImageService.GetBlobUrlAsync(blobKey);

            if (blobUrl != null)
            {
                FeedService.InitializeImage(id, blobKey, blobUrl);
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

    private static string GetCropStyle(CropData? crop)
    {
        if (crop == null || crop.Scale == 0)
        {
            return "width:324px; height:405px; position:absolute; object-fit: scale-down";
        }

        // 0.81 - Current container size / preview size TODO: potentially provide cropper container size
        return $"position:absolute;" +
               $"transform:translate({crop.PosX * 0.81}px, {crop.PosY * 0.81}px) scale({crop.Scale * 0.81});" +
               $"transform-origin:top left;";
    }

    private async Task OnCropConfirmed((string _, CropDataModel cropData) result)
    {
        if (adjustingElement != null)
        {
            FeedService.UpdateCropDetails(adjustingElement!.Id, result.cropData.ToCropData());

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