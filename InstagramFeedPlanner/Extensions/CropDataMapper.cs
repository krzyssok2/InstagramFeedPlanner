using InstagramFeedPlanner.IndexedDB.Models;
using InstagramFeedPlanner.Models;

namespace InstagramFeedPlanner.Extensions;

public static class CropDataMapper
{
    public static CropDataModel ToCropDataModel(this CropData cropData)
    {
        if (cropData == null)
        {
            return null;
        }

        return new CropDataModel
        {
            PosX = cropData.PosX,
            PosY = cropData.PosY,
            Scale = cropData.Scale,
            ZoomValue = cropData.ZoomValue
        };
    }

    public static CropData ToCropData(this CropDataModel cropDataModel)
    {
        if (cropDataModel == null)
        {
            return null;
        }

        return new CropData
        {
            PosX = cropDataModel.PosX,
            PosY = cropDataModel.PosY,
            Scale = cropDataModel.Scale,
            ZoomValue = cropDataModel.ZoomValue
        };
    }
}