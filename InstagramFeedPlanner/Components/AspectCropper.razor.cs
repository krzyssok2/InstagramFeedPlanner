using InstagramFeedPlanner.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace InstagramFeedPlanner.Components;

public partial class AspectCropper
{
    [Parameter] public string Src { get; set; } = "";
    [Parameter] public EventCallback<CropDataModel> OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public CropDataModel? CropData { get; set; }
    [Parameter] public double AspectRatio { get; set; } = 1.0; // width/height
    [Parameter] public double ViewportPercentage { get; set; } = 0.6; // 60% of viewport

    private ElementReference ImageElement;

    // Image natural dimensions
    private double NaturalWidth = 800;

    private double NaturalHeight = 600;

    // Crop area dimensions
    private double CropWidth;

    private double CropHeight;

    // Original crop dimensions (for scaling calculations)
    private double OriginalCropWidth;

    private double OriginalCropHeight;

    // Current transform values
    private double Scale = 1.0;

    private double PosX = 0;
    private double PosY = 0;
    private double ZoomValue = 0; // 0-100 for slider

    // Scale bounds
    private double MinScale = 1.0;

    private double MaxScale = 3.0;
    private int MinZoom = 0;
    private int MaxZoom = 100;

    // Calculated display dimensions
    private double DisplayWidth => NaturalWidth * Scale;

    private double DisplayHeight => NaturalHeight * Scale;

    // Mouse/touch dragging
    private bool IsDragging = false;

    private double DragStartX = 0;
    private double DragStartY = 0;
    private double DragStartPosX = 0;
    private double DragStartPosY = 0;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadImageDimensions();
            await CalculateResponsiveCropDimensions();
            CalculateScaleBounds();
            RestoreCropData();
            CenterImageIfNeeded();
            await SetupWindowResize();
            StateHasChanged();
        }
    }

    private async Task LoadImageDimensions()
    {
        try
        {
            var dimensions = await JS.InvokeAsync<ImageDimensions>("getImageDimensions", Src);
            NaturalWidth = dimensions.Width;
            NaturalHeight = dimensions.Height;
        }
        catch
        {
            // Fallback to default dimensions
            NaturalWidth = 800;
            NaturalHeight = 600;
        }
    }

    private async Task CalculateResponsiveCropDimensions()
    {
        try
        {
            var windowSize = await JS.InvokeAsync<WindowSize>("getWindowSize");
            var maxWidth = windowSize.Width * ViewportPercentage;
            var maxHeight = windowSize.Height * ViewportPercentage;

            if (AspectRatio >= 1)
            {
                // Landscape: try to use width constraint first
                CropWidth = System.Math.Min(maxWidth, maxHeight * AspectRatio);
                CropHeight = CropWidth / AspectRatio;

                // If height is too big, constraint by height instead
                if (CropHeight > maxHeight)
                {
                    CropHeight = maxHeight;
                    CropWidth = CropHeight * AspectRatio;
                }
            }
            else
            {
                // Portrait: try to use height constraint first
                CropHeight = System.Math.Min(maxHeight, maxWidth / AspectRatio);
                CropWidth = CropHeight * AspectRatio;

                // If width is too big, constraint by width instead
                if (CropWidth > maxWidth)
                {
                    CropWidth = maxWidth;
                    CropHeight = CropWidth / AspectRatio;
                }
            }

            // Store original dimensions if not set yet
            if (OriginalCropWidth == 0)
            {
                OriginalCropWidth = CropWidth;
                OriginalCropHeight = CropHeight;
            }
        }
        catch
        {
            // Fallback dimensions
            CropWidth = 400;
            CropHeight = 400 / AspectRatio;
            if (OriginalCropWidth == 0)
            {
                OriginalCropWidth = CropWidth;
                OriginalCropHeight = CropHeight;
            }
        }
    }

    private async Task SetupWindowResize()
    {
        await JS.InvokeVoidAsync("setupCropperResize", DotNetObjectReference.Create(this));
    }

    private void CalculateScaleBounds()
    {
        // Minimum scale ensures the image covers the entire crop area
        var scaleX = CropWidth / NaturalWidth;
        var scaleY = CropHeight / NaturalHeight;
        MinScale = System.Math.Max(scaleX, scaleY);

        // Maximum scale is 3x the minimum
        MaxScale = MinScale * 3.0;
    }

    private void RestoreCropData()
    {
        if (CropData != null && CropData.Scale > 0)
        {
            // Calculate scaling factors based on crop area size changes
            var scaleFactorX = CropWidth / CropData.PreviewWidth;
            var scaleFactorY = CropHeight / CropData.PreviewHeight;

            // Use the average scale factor to maintain aspect ratio
            var avgScaleFactor = (scaleFactorX + scaleFactorY) / 2.0;

            // Adjust position and scale based on current window size
            PosX = CropData.PosX * scaleFactorX;
            PosY = CropData.PosY * scaleFactorY;

            // Adjust scale - first calculate what the original scale was relative to the original MinScale
            var originalMinScale = System.Math.Max(CropData.PreviewWidth / NaturalWidth, CropData.PreviewHeight / NaturalHeight);
            var relativeScale = CropData.Scale / originalMinScale;
            Scale = MinScale * relativeScale;

            // Ensure scale is within current bounds
            Scale = System.Math.Max(MinScale, System.Math.Min(MaxScale, Scale));

            // Calculate zoom slider position
            var normalizedScale = (Scale - MinScale) / (MaxScale - MinScale);
            ZoomValue = normalizedScale * 100;
        }
        else
        {
            Scale = MinScale;
            ZoomValue = 0;
        }

        ClampPosition();
    }

    private void CenterImageIfNeeded()
    {
        if (CropData == null || CropData.Scale <= 0)
        {
            // Center the image initially
            PosX = (CropWidth - DisplayWidth) / 2;
            PosY = (CropHeight - DisplayHeight) / 2;
            ClampPosition();
        }
    }

    private void OnZoomChange(ChangeEventArgs e)
    {
        ZoomValue = Convert.ToDouble(e.Value);
        var oldScale = Scale;

        // Calculate new scale
        var normalizedZoom = ZoomValue / 100.0;
        Scale = MinScale + normalizedZoom * (MaxScale - MinScale);

        // Keep the center point stable during zoom
        var centerX = CropWidth / 2.0;
        var centerY = CropHeight / 2.0;

        var imageCenterX = (centerX - PosX) / oldScale;
        var imageCenterY = (centerY - PosY) / oldScale;

        PosX = centerX - imageCenterX * Scale;
        PosY = centerY - imageCenterY * Scale;

        ClampPosition();
        StateHasChanged();
    }

    private void StartDrag(MouseEventArgs e)
    {
        IsDragging = true;
        DragStartX = e.ClientX;
        DragStartY = e.ClientY;
        DragStartPosX = PosX;
        DragStartPosY = PosY;
    }

    private void StartDrag(TouchEventArgs e)
    {
        if (e.Touches.Length > 0)
        {
            IsDragging = true;
            DragStartX = e.Touches[0].ClientX;
            DragStartY = e.Touches[0].ClientY;
            DragStartPosX = PosX;
            DragStartPosY = PosY;
        }
    }

    private void OnDrag(MouseEventArgs e)
    {
        if (!IsDragging) return;

        var deltaX = e.ClientX - DragStartX;
        var deltaY = e.ClientY - DragStartY;

        PosX = DragStartPosX + deltaX;
        PosY = DragStartPosY + deltaY;

        ClampPosition();
        StateHasChanged();
    }

    private void OnDrag(TouchEventArgs e)
    {
        if (!IsDragging || e.Touches.Length == 0) return;

        var deltaX = e.Touches[0].ClientX - DragStartX;
        var deltaY = e.Touches[0].ClientY - DragStartY;

        PosX = DragStartPosX + deltaX;
        PosY = DragStartPosY + deltaY;

        ClampPosition();
        StateHasChanged();
    }

    private void EndDrag(MouseEventArgs e)
    {
        IsDragging = false;
    }

    private void EndDrag(TouchEventArgs e)
    {
        IsDragging = false;
    }

    private void ClampPosition()
    {
        // Ensure image doesn't go beyond crop boundaries
        var minX = CropWidth - DisplayWidth;
        var minY = CropHeight - DisplayHeight;

        PosX = System.Math.Min(0, System.Math.Max(minX, PosX));
        PosY = System.Math.Min(0, System.Math.Max(minY, PosY));
    }

    private async Task ConfirmCrop()
    {
        var cropData = new CropDataModel
        {
            PosX = PosX,
            PosY = PosY,
            Scale = Scale,
            ZoomValue = ZoomValue,
            PreviewWidth = CropWidth,
            PreviewHeight = CropHeight
        };

        await OnConfirm.InvokeAsync(cropData);
    }

    private async Task Cancel()
    {
        await OnCancel.InvokeAsync();
    }

    // Method to get normalized crop data for consistent preview across different window sizes
    public CropDataModel GetNormalizedCropData()
    {
        // Calculate crop data relative to crop area dimensions for consistent preview
        var normalizedPosX = PosX / CropWidth;
        var normalizedPosY = PosY / CropHeight;
        var normalizedScale = Scale / MinScale;

        return new CropDataModel
        {
            PosX = normalizedPosX,
            PosY = normalizedPosY,
            Scale = normalizedScale,
            ZoomValue = ZoomValue,
            PreviewWidth = CropWidth,
            PreviewHeight = CropHeight
        };
    }

    // Method to apply normalized crop data (expects values between 0-1 for position ratios)
    public void ApplyNormalizedCropData(CropDataModel normalizedData)
    {
        // Check if this looks like normalized data (position values between 0-1)
        if (System.Math.Abs(normalizedData.PosX) <= 1.0 && System.Math.Abs(normalizedData.PosY) <= 1.0 &&
            normalizedData.Scale <= 10.0)
        {
            PosX = normalizedData.PosX * CropWidth;
            PosY = normalizedData.PosY * CropHeight;
            Scale = normalizedData.Scale * MinScale;
            ZoomValue = normalizedData.ZoomValue;

            ClampPosition();
            StateHasChanged();
        }
    }

    private int GetDisplayZoomPercentage()
    {
        return (int)((Scale / MinScale) * 100);
    }

    public async ValueTask DisposeAsync()
    {
        await JS.InvokeVoidAsync("cleanupCropperResize");
    }

    [JSInvokable]
    public async Task OnWindowResize()
    {
        var oldCropWidth = CropWidth;
        var oldCropHeight = CropHeight;
        var oldMinScale = MinScale;

        await CalculateResponsiveCropDimensions();

        // If size changed significantly, recalculate everything
        if (System.Math.Abs(CropWidth - oldCropWidth) > 10 ||
            System.Math.Abs(CropHeight - oldCropHeight) > 10)
        {
            // Store current position as ratios
            var posXRatio = PosX / oldCropWidth;
            var posYRatio = PosY / oldCropHeight;

            // Recalculate scale bounds for new crop dimensions
            CalculateScaleBounds();

            // Adjust current scale proportionally
            var scaleRatio = MinScale / oldMinScale;
            Scale *= scaleRatio;

            // Ensure scale is within new bounds
            Scale = System.Math.Max(MinScale, System.Math.Min(MaxScale, Scale));

            // Recalculate zoom slider position based on new scale bounds
            var normalizedScale = (Scale - MinScale) / (MaxScale - MinScale);
            ZoomValue = normalizedScale * 100;

            // Apply position ratios to new dimensions
            PosX = posXRatio * CropWidth;
            PosY = posYRatio * CropHeight;

            ClampPosition();
            StateHasChanged();
        }
    }

    private class ImageDimensions
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    private class WindowSize
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }
}