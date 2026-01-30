using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Storage;

namespace MotoRent.Services;

/// <summary>
/// Service for managing fleet model marketing/catalog images.
/// Supports up to 5 images per fleet model with primary image selection.
/// </summary>
public class FleetModelImageService(RentalDataContext context, IBinaryStore binaryStore)
{
    private RentalDataContext Context { get; } = context;
    private IBinaryStore BinaryStore { get; } = binaryStore;

    public const int MaxImagesPerFleetModel = 5;

    #region Query Methods

    public async Task<List<FleetModelImage>> GetImagesForFleetModelAsync(int fleetModelId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<FleetModelImage>()
                .Where(fi => fi.FleetModelId == fleetModelId)
                .OrderBy(fi => fi.DisplayOrder),
            page: 1, size: MaxImagesPerFleetModel, includeTotalRows: false);

        return result.ItemCollection;
    }

    public async Task<FleetModelImage?> GetPrimaryImageAsync(int fleetModelId)
    {
        var images = await GetImagesForFleetModelAsync(fleetModelId);
        return images.FirstOrDefault(i => i.IsPrimary) ?? images.FirstOrDefault();
    }

    public async Task<string?> GetPrimaryImageStoreIdAsync(int fleetModelId)
    {
        var primary = await GetPrimaryImageAsync(fleetModelId);
        return primary?.StoreId;
    }

    public async Task<int> GetImageCountAsync(int fleetModelId)
    {
        return await this.Context.GetCountAsync(
            this.Context.CreateQuery<FleetModelImage>().Where(fi => fi.FleetModelId == fleetModelId));
    }

    #endregion

    #region CRUD Operations

    public async Task<SubmitOperation> AddImageAsync(int fleetModelId, string storeId, string? caption, string username)
    {
        var existingCount = await GetImageCountAsync(fleetModelId);

        if (existingCount >= MaxImagesPerFleetModel)
        {
            return SubmitOperation.CreateFailure(
                $"Maximum of {MaxImagesPerFleetModel} images allowed per fleet model.");
        }

        var image = new FleetModelImage
        {
            FleetModelId = fleetModelId,
            StoreId = storeId,
            IsPrimary = existingCount == 0,
            DisplayOrder = existingCount + 1,
            Caption = caption,
            UploadedOn = DateTimeOffset.UtcNow
        };

        using var session = this.Context.OpenSession(username);
        session.Attach(image);
        return await session.SubmitChanges("AddFleetModelImage");
    }

    public async Task<SubmitOperation> SetPrimaryImageAsync(int fleetModelId, int fleetModelImageId, string username)
    {
        var images = await GetImagesForFleetModelAsync(fleetModelId);

        if (images.Count == 0)
        {
            return SubmitOperation.CreateFailure("No images found for fleet model.");
        }

        var targetImage = images.FirstOrDefault(i => i.FleetModelImageId == fleetModelImageId);
        if (targetImage == null)
        {
            return SubmitOperation.CreateFailure("Image not found.");
        }

        if (targetImage.IsPrimary)
        {
            return SubmitOperation.CreateSuccess(0, 0, 0);
        }

        using var session = this.Context.OpenSession(username);

        foreach (var img in images)
        {
            var wasPrimary = img.IsPrimary;
            img.IsPrimary = img.FleetModelImageId == fleetModelImageId;

            if (wasPrimary != img.IsPrimary)
            {
                session.Attach(img);
            }
        }

        return await session.SubmitChanges("SetPrimaryFleetModelImage");
    }

    public async Task<SubmitOperation> DeleteImageAsync(int fleetModelImageId, string username)
    {
        var image = await this.Context.LoadOneAsync<FleetModelImage>(
            fi => fi.FleetModelImageId == fleetModelImageId);

        if (image == null)
        {
            return SubmitOperation.CreateFailure("Image not found.");
        }

        var fleetModelId = image.FleetModelId;
        var wasPrimary = image.IsPrimary;

        if (!string.IsNullOrEmpty(image.StoreId))
        {
            try
            {
                await this.BinaryStore.DeleteAsync(image.StoreId);
            }
            catch
            {
                // Log warning but continue with database delete
            }
        }

        using var session = this.Context.OpenSession(username);
        session.Delete(image);
        var result = await session.SubmitChanges("DeleteFleetModelImage");

        if (!result.Success)
        {
            return result;
        }

        var remainingImages = await GetImagesForFleetModelAsync(fleetModelId);

        if (remainingImages.Count > 0)
        {
            using var reorderSession = this.Context.OpenSession(username);
            var attachedImages = new HashSet<int>();
            var hasChanges = false;

            if (wasPrimary)
            {
                remainingImages[0].IsPrimary = true;
                reorderSession.Attach(remainingImages[0]);
                attachedImages.Add(remainingImages[0].FleetModelImageId);
                hasChanges = true;
            }

            for (int i = 0; i < remainingImages.Count; i++)
            {
                var img = remainingImages[i];
                if (img.DisplayOrder != i + 1)
                {
                    img.DisplayOrder = i + 1;
                    if (!attachedImages.Contains(img.FleetModelImageId))
                    {
                        reorderSession.Attach(img);
                        attachedImages.Add(img.FleetModelImageId);
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                await reorderSession.SubmitChanges("ReorderAfterDelete");
            }
        }

        return result;
    }

    public async Task<SubmitOperation> UpdateCaptionAsync(int fleetModelImageId, string? caption, string username)
    {
        var image = await this.Context.LoadOneAsync<FleetModelImage>(
            fi => fi.FleetModelImageId == fleetModelImageId);

        if (image == null)
        {
            return SubmitOperation.CreateFailure("Image not found.");
        }

        image.Caption = caption;

        using var session = this.Context.OpenSession(username);
        session.Attach(image);
        return await session.SubmitChanges("UpdateFleetModelImageCaption");
    }

    public async Task<SubmitOperation> ReorderImagesAsync(int fleetModelId, List<int> orderedImageIds, string username)
    {
        var images = await GetImagesForFleetModelAsync(fleetModelId);

        if (images.Count == 0)
        {
            return SubmitOperation.CreateSuccess(0, 0, 0);
        }

        using var session = this.Context.OpenSession(username);

        for (int i = 0; i < orderedImageIds.Count; i++)
        {
            var imageId = orderedImageIds[i];
            var image = images.FirstOrDefault(img => img.FleetModelImageId == imageId);

            if (image != null && image.DisplayOrder != i + 1)
            {
                image.DisplayOrder = i + 1;
                session.Attach(image);
            }
        }

        return await session.SubmitChanges("ReorderFleetModelImages");
    }

    public async Task<SubmitOperation> DeleteAllImagesAsync(int fleetModelId, string username)
    {
        var images = await GetImagesForFleetModelAsync(fleetModelId);

        if (images.Count == 0)
        {
            return SubmitOperation.CreateSuccess(0, 0, 0);
        }

        foreach (var image in images)
        {
            if (!string.IsNullOrEmpty(image.StoreId))
            {
                try
                {
                    await this.BinaryStore.DeleteAsync(image.StoreId);
                }
                catch
                {
                    // Log warning but continue
                }
            }
        }

        using var session = this.Context.OpenSession(username);
        foreach (var image in images)
        {
            session.Delete(image);
        }

        return await session.SubmitChanges("DeleteAllFleetModelImages");
    }

    #endregion
}
