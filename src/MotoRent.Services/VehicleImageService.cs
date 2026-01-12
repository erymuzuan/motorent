using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Storage;

namespace MotoRent.Services;

/// <summary>
/// Service for managing vehicle images.
/// Supports up to 5 images per vehicle with primary image selection.
/// </summary>
public class VehicleImageService(RentalDataContext context, IBinaryStore binaryStore)
{
    private RentalDataContext Context { get; } = context;
    private IBinaryStore BinaryStore { get; } = binaryStore;

    /// <summary>
    /// Maximum number of images allowed per vehicle.
    /// </summary>
    public const int MaxImagesPerVehicle = 5;

    #region Query Methods

    /// <summary>
    /// Gets all images for a vehicle, ordered by display order.
    /// </summary>
    public async Task<List<VehicleImage>> GetImagesForVehicleAsync(int vehicleId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<VehicleImage>()
                .Where(vi => vi.VehicleId == vehicleId)
                .OrderBy(vi => vi.DisplayOrder),
            page: 1, size: MaxImagesPerVehicle, includeTotalRows: false);

        return result.ItemCollection;
    }

    /// <summary>
    /// Gets the primary image for a vehicle.
    /// </summary>
    public async Task<VehicleImage?> GetPrimaryImageAsync(int vehicleId)
    {
        var images = await GetImagesForVehicleAsync(vehicleId);
        return images.FirstOrDefault(i => i.IsPrimary) ?? images.FirstOrDefault();
    }

    /// <summary>
    /// Gets the primary image store ID for a vehicle.
    /// </summary>
    public async Task<string?> GetPrimaryImageStoreIdAsync(int vehicleId)
    {
        var primary = await GetPrimaryImageAsync(vehicleId);
        return primary?.StoreId;
    }

    /// <summary>
    /// Gets the count of images for a vehicle.
    /// </summary>
    public async Task<int> GetImageCountAsync(int vehicleId)
    {
        return await this.Context.GetCountAsync(
            this.Context.CreateQuery<VehicleImage>().Where(vi => vi.VehicleId == vehicleId));
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Adds a new image to a vehicle.
    /// </summary>
    public async Task<SubmitOperation> AddImageAsync(int vehicleId, string storeId, string? caption, string username)
    {
        // Check max images constraint
        var existingCount = await GetImageCountAsync(vehicleId);

        if (existingCount >= MaxImagesPerVehicle)
        {
            return SubmitOperation.CreateFailure(
                $"Maximum of {MaxImagesPerVehicle} images allowed per vehicle.");
        }

        var image = new VehicleImage
        {
            VehicleId = vehicleId,
            StoreId = storeId,
            IsPrimary = existingCount == 0, // First image is primary by default
            DisplayOrder = existingCount + 1,
            Caption = caption,
            UploadedOn = DateTimeOffset.UtcNow
        };

        using var session = this.Context.OpenSession(username);
        session.Attach(image);
        return await session.SubmitChanges("AddImage");
    }

    /// <summary>
    /// Sets an image as the primary image for a vehicle.
    /// </summary>
    public async Task<SubmitOperation> SetPrimaryImageAsync(int vehicleId, int vehicleImageId, string username)
    {
        var images = await GetImagesForVehicleAsync(vehicleId);

        if (images.Count == 0)
        {
            return SubmitOperation.CreateFailure("No images found for vehicle.");
        }

        var targetImage = images.FirstOrDefault(i => i.VehicleImageId == vehicleImageId);
        if (targetImage == null)
        {
            return SubmitOperation.CreateFailure("Image not found.");
        }

        if (targetImage.IsPrimary)
        {
            return SubmitOperation.CreateSuccess(0, 0, 0); // Already primary, no-op
        }

        using var session = this.Context.OpenSession(username);

        foreach (var img in images)
        {
            var wasPrimary = img.IsPrimary;
            img.IsPrimary = img.VehicleImageId == vehicleImageId;

            // Only attach if changed
            if (wasPrimary != img.IsPrimary)
            {
                session.Attach(img);
            }
        }

        return await session.SubmitChanges("SetPrimaryImage");
    }

    /// <summary>
    /// Deletes an image from a vehicle.
    /// </summary>
    public async Task<SubmitOperation> DeleteImageAsync(int vehicleImageId, string username)
    {
        var image = await this.Context.LoadOneAsync<VehicleImage>(
            vi => vi.VehicleImageId == vehicleImageId);

        if (image == null)
        {
            return SubmitOperation.CreateFailure("Image not found.");
        }

        var vehicleId = image.VehicleId;
        var wasPrimary = image.IsPrimary;
        var deletedOrder = image.DisplayOrder;

        // Delete from binary store
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

        // Delete from database
        using var session = this.Context.OpenSession(username);
        session.Delete(image);
        var result = await session.SubmitChanges("DeleteImage");

        if (!result.Success)
        {
            return result;
        }

        // Re-fetch remaining images
        var remainingImages = await GetImagesForVehicleAsync(vehicleId);

        if (remainingImages.Count > 0)
        {
            using var reorderSession = this.Context.OpenSession(username);
            var attachedImages = new HashSet<int>();
            var hasChanges = false;

            // If deleted image was primary, set next image as primary
            if (wasPrimary)
            {
                remainingImages[0].IsPrimary = true;
                reorderSession.Attach(remainingImages[0]);
                attachedImages.Add(remainingImages[0].VehicleImageId);
                hasChanges = true;
            }

            // Reorder remaining images to fill the gap
            for (int i = 0; i < remainingImages.Count; i++)
            {
                var img = remainingImages[i];
                if (img.DisplayOrder != i + 1)
                {
                    img.DisplayOrder = i + 1;
                    if (!attachedImages.Contains(img.VehicleImageId))
                    {
                        reorderSession.Attach(img);
                        attachedImages.Add(img.VehicleImageId);
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

    /// <summary>
    /// Updates an image's caption.
    /// </summary>
    public async Task<SubmitOperation> UpdateCaptionAsync(int vehicleImageId, string? caption, string username)
    {
        var image = await this.Context.LoadOneAsync<VehicleImage>(
            vi => vi.VehicleImageId == vehicleImageId);

        if (image == null)
        {
            return SubmitOperation.CreateFailure("Image not found.");
        }

        image.Caption = caption;

        using var session = this.Context.OpenSession(username);
        session.Attach(image);
        return await session.SubmitChanges("UpdateCaption");
    }

    /// <summary>
    /// Reorders images for a vehicle.
    /// </summary>
    public async Task<SubmitOperation> ReorderImagesAsync(int vehicleId, List<int> orderedImageIds, string username)
    {
        var images = await GetImagesForVehicleAsync(vehicleId);

        if (images.Count == 0)
        {
            return SubmitOperation.CreateSuccess(0, 0, 0);
        }

        using var session = this.Context.OpenSession(username);

        for (int i = 0; i < orderedImageIds.Count; i++)
        {
            var imageId = orderedImageIds[i];
            var image = images.FirstOrDefault(img => img.VehicleImageId == imageId);

            if (image != null && image.DisplayOrder != i + 1)
            {
                image.DisplayOrder = i + 1;
                session.Attach(image);
            }
        }

        return await session.SubmitChanges("ReorderImages");
    }

    /// <summary>
    /// Deletes all images for a vehicle.
    /// </summary>
    public async Task<SubmitOperation> DeleteAllImagesAsync(int vehicleId, string username)
    {
        var images = await GetImagesForVehicleAsync(vehicleId);

        if (images.Count == 0)
        {
            return SubmitOperation.CreateSuccess(0, 0, 0);
        }

        // Delete from binary store
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

        // Delete from database
        using var session = this.Context.OpenSession(username);
        foreach (var image in images)
        {
            session.Delete(image);
        }

        return await session.SubmitChanges("DeleteAllImages");
    }

    #endregion
}
