using InstagramFeedPlanner.Extensions;
using InstagramFeedPlanner.Models;
using InstagramFeedPlanner.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace InstagramFeedPlanner.Pages;

public partial class Planner(IJSRuntime js, UserFeedService Feed, IndexedDbImageService indexedDbImageService)
{
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Feed.Initialize();

            StateHasChanged();
        }
    }

    private Guid? draggedItemId;
    private Post? adjustingElement;

    private void AddElement() => Feed.AddEmptyPost();

    private void OnPostDelete(Guid id) => Feed.DeletePost(id);

    private void OnAdjust(Post element) => adjustingElement = element;

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
                Feed.InitializeImage(id, blobKey, blobUrl);
                return;
            }
            StateHasChanged();
        }
    }

    private void HandlePostDrop(Guid draggedPost, Guid targetPost, DragEventArgs e)
    {
        if (e.ShiftKey)
        {
            Feed.SwapPosts(draggedPost, targetPost);
        }
        else
        {
            Feed.InsertPostIntoPosition(draggedPost, targetPost);
        }
    }

    private string GetCropStyle(CropData crop)
    {
        if (crop == null || crop.Scale == 0)
        {
            return "object-fit:contain";
        }

        Console.WriteLine(crop.Scale);

        // 0.81 - Current container size / preview size TODO: potentially provide cropper container size
        return $"position:absolute;" +
               $"transform:translate({crop.PosX * 0.81}px, {crop.PosY * 0.81}px) scale({crop.Scale * 0.81});" +
               $"transform-origin:top left;";
    }

    private async Task OnCropConfirmed((string _, CropDataModel cropData) result)
    {
        if (adjustingElement != null)
        {
            Feed.UpdateCropDetails(adjustingElement!.Id, result.cropData.ToCropData());

            adjustingElement = null;
            StateHasChanged();
        }
    }
}