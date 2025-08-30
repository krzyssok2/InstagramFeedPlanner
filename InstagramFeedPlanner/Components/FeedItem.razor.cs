using InstagramFeedPlanner.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace InstagramFeedPlanner.Components;

public partial class FeedItem
{
    private ElementReference FeedItemRef;
    private double FeedItemWidth;
    private double FeedItemHeight;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("resizeObserver.observe", FeedItemRef, DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public void OnResized(double width, double height)
    {
        FeedItemWidth = width;
        FeedItemHeight = height;
        StateHasChanged();
    }

    [Parameter] public Post Post { get; set; } = null!;
    [Parameter] public bool IsSelected { get; set; } = false;
    [Parameter] public EventCallback<Guid> OnDelete { get; set; }
    [Parameter] public EventCallback<Guid> OnToggleLock { get; set; }
    [Parameter] public EventCallback<Post> OnAdjust { get; set; }
    [Parameter] public EventCallback<Guid> OnDragStart { get; set; }
    [Parameter] public EventCallback<(Guid PostId, DragEventArgs Args)> OnDrop { get; set; }
    [Parameter] public EventCallback<(Guid PostId, string Url)> OnUpload { get; set; }
    [Parameter] public EventCallback<(Guid PostId, MouseEventArgs Args)> OnClick { get; set; }

    private void OnPostClick(MouseEventArgs args, Guid Id) => OnClick.InvokeAsync((Id, args));

    private string GetCropStyle(CropData? crop, double containerWidth, double containerHeight)
    {
        if (crop == null || crop.Scale == 0)
        {
            return $"width:{FeedItemWidth}px; height:{FeedItemHeight}px; position:absolute; object-fit: cover";
        }

        var sizeAdjustment = containerWidth / crop.PreviewWidth;

        return $"position:absolute;" +
               $"transform:translate({crop.PosX * sizeAdjustment}px, {crop.PosY * sizeAdjustment}px) scale({crop.Scale * sizeAdjustment});" +
               $"transform-origin:top left;";
    }

    private async Task Import(InputFileChangeEventArgs args, Guid guid)
    {
        if (args.FileCount != 1) return;

        var file = args.File;

        using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        var base64 = Convert.ToBase64String(ms.ToArray());
        var dataUrl = $"data:{file.ContentType};base64,{base64}";

        await OnUpload.InvokeAsync((guid, dataUrl));
    }
}