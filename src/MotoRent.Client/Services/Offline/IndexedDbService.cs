using System.Text.Json;
using Microsoft.JSInterop;

namespace MotoRent.Client.Services.Offline;

/// <summary>
/// Service for interacting with IndexedDB via JavaScript interop.
/// Provides offline storage capabilities for the tourist PWA.
/// </summary>
public class IndexedDbService : IAsyncDisposable
{
    private readonly IJSRuntime m_jsRuntime;
    private readonly JsonSerializerOptions m_jsonOptions;
    private bool m_initialized;

    public IndexedDbService(IJSRuntime jsRuntime)
    {
        m_jsRuntime = jsRuntime;
        m_jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Initialize the IndexedDB database.
    /// Includes retry logic for timing issues with JS module loading.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (m_initialized) return;

        const int maxRetries = 5;
        const int retryDelayMs = 200;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await m_jsRuntime.InvokeVoidAsync("MotoRentOfflineDb.init");
                m_initialized = true;
                return;
            }
            catch (JSException ex) when (ex.Message.Contains("was undefined") && attempt < maxRetries)
            {
                // JS module not loaded yet, wait and retry
                Console.WriteLine($"[IndexedDbService] Init attempt {attempt}/{maxRetries} - waiting for JS module...");
                await Task.Delay(retryDelayMs * attempt);
            }
            catch (JSException ex)
            {
                Console.WriteLine($"[IndexedDbService] Init error: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Check if IndexedDB is supported in the browser.
    /// </summary>
    public async Task<bool> IsSupportedAsync()
    {
        try
        {
            return await m_jsRuntime.InvokeAsync<bool>("MotoRentOfflineDb.isSupported");
        }
        catch
        {
            return false;
        }
    }

    #region Generic CRUD Operations

    /// <summary>
    /// Get a single item by key from a store.
    /// </summary>
    public async Task<T?> GetAsync<T>(string storeName, object key) where T : class
    {
        await InitializeAsync();
        try
        {
            var result = await m_jsRuntime.InvokeAsync<JsonElement?>("MotoRentOfflineDb.get", storeName, key);
            if (result == null || result.Value.ValueKind == JsonValueKind.Null)
                return null;

            return JsonSerializer.Deserialize<T>(result.Value.GetRawText(), m_jsonOptions);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] Get error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get all items from a store.
    /// </summary>
    public async Task<List<T>> GetAllAsync<T>(string storeName) where T : class
    {
        await InitializeAsync();
        try
        {
            var result = await m_jsRuntime.InvokeAsync<JsonElement>("MotoRentOfflineDb.getAll", storeName);
            if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
                return [];

            return JsonSerializer.Deserialize<List<T>>(result.GetRawText(), m_jsonOptions) ?? [];
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] GetAll error: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Get items by index value.
    /// </summary>
    public async Task<List<T>> GetByIndexAsync<T>(string storeName, string indexName, object value) where T : class
    {
        await InitializeAsync();
        try
        {
            var result = await m_jsRuntime.InvokeAsync<JsonElement>("MotoRentOfflineDb.getByIndex", storeName, indexName, value);
            if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
                return [];

            return JsonSerializer.Deserialize<List<T>>(result.GetRawText(), m_jsonOptions) ?? [];
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] GetByIndex error: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Put (insert or update) an item in a store.
    /// </summary>
    public async Task PutAsync<T>(string storeName, T item) where T : class
    {
        await InitializeAsync();
        try
        {
            var json = JsonSerializer.Serialize(item, m_jsonOptions);
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            await m_jsRuntime.InvokeVoidAsync("MotoRentOfflineDb.put", storeName, element);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] Put error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Put multiple items in a store.
    /// </summary>
    public async Task PutManyAsync<T>(string storeName, IEnumerable<T> items) where T : class
    {
        await InitializeAsync();
        try
        {
            var json = JsonSerializer.Serialize(items, m_jsonOptions);
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            await m_jsRuntime.InvokeVoidAsync("MotoRentOfflineDb.putMany", storeName, element);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] PutMany error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Delete an item by key from a store.
    /// </summary>
    public async Task<bool> DeleteAsync(string storeName, object key)
    {
        await InitializeAsync();
        try
        {
            return await m_jsRuntime.InvokeAsync<bool>("MotoRentOfflineDb.remove", storeName, key);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] Delete error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clear all items from a store.
    /// </summary>
    public async Task ClearAsync(string storeName)
    {
        await InitializeAsync();
        try
        {
            await m_jsRuntime.InvokeVoidAsync("MotoRentOfflineDb.clear", storeName);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] Clear error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Count items in a store.
    /// </summary>
    public async Task<int> CountAsync(string storeName)
    {
        await InitializeAsync();
        try
        {
            return await m_jsRuntime.InvokeAsync<int>("MotoRentOfflineDb.count", storeName);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] Count error: {ex.Message}");
            return 0;
        }
    }

    #endregion

    #region Rental Operations

    /// <summary>
    /// Save a complete rental package for offline use.
    /// </summary>
    public async Task SaveRentalPackageAsync(OfflineRentalPackage package)
    {
        await InitializeAsync();
        try
        {
            var json = JsonSerializer.Serialize(package, m_jsonOptions);
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            await m_jsRuntime.InvokeVoidAsync("MotoRentOfflineDb.saveRentalPackage", element);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] SaveRentalPackage error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get the active rental from offline storage.
    /// </summary>
    public async Task<OfflineRentalPackage?> GetActiveRentalAsync()
    {
        await InitializeAsync();
        try
        {
            var result = await m_jsRuntime.InvokeAsync<JsonElement?>("MotoRentOfflineDb.getActiveRental");
            if (result == null || result.Value.ValueKind == JsonValueKind.Null)
                return null;

            return JsonSerializer.Deserialize<OfflineRentalPackage>(result.Value.GetRawText(), m_jsonOptions);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] GetActiveRental error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get rental by WebId from offline storage.
    /// </summary>
    public async Task<OfflineRentalPackage?> GetRentalByWebIdAsync(string webId)
    {
        await InitializeAsync();
        try
        {
            var result = await m_jsRuntime.InvokeAsync<JsonElement?>("MotoRentOfflineDb.getRentalByWebId", webId);
            if (result == null || result.Value.ValueKind == JsonValueKind.Null)
                return null;

            return JsonSerializer.Deserialize<OfflineRentalPackage>(result.Value.GetRawText(), m_jsonOptions);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] GetRentalByWebId error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if rental data is stale and needs refresh.
    /// </summary>
    public async Task<bool> IsRentalStaleAsync(int rentalId, int maxAgeMinutes = 60)
    {
        await InitializeAsync();
        try
        {
            return await m_jsRuntime.InvokeAsync<bool>("MotoRentOfflineDb.isRentalStale", rentalId, maxAgeMinutes);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] IsRentalStale error: {ex.Message}");
            return true; // Assume stale on error
        }
    }

    #endregion

    #region Photo Operations

    /// <summary>
    /// Save a photo for later sync.
    /// </summary>
    public async Task<string> SavePendingPhotoAsync(OfflinePendingPhoto photo)
    {
        await InitializeAsync();
        try
        {
            var json = JsonSerializer.Serialize(photo, m_jsonOptions);
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return await m_jsRuntime.InvokeAsync<string>("MotoRentOfflineDb.savePendingPhoto", element);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] SavePendingPhoto error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get pending (unsynced) photos for a rental.
    /// </summary>
    public async Task<List<OfflinePendingPhoto>> GetPendingPhotosAsync(int? rentalId = null)
    {
        await InitializeAsync();
        try
        {
            var result = rentalId.HasValue
                ? await m_jsRuntime.InvokeAsync<JsonElement>("MotoRentOfflineDb.getPendingPhotos", rentalId.Value)
                : await m_jsRuntime.InvokeAsync<JsonElement>("MotoRentOfflineDb.getPendingPhotos");

            if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
                return [];

            return JsonSerializer.Deserialize<List<OfflinePendingPhoto>>(result.GetRawText(), m_jsonOptions) ?? [];
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] GetPendingPhotos error: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Mark a photo as synced.
    /// </summary>
    public async Task MarkPhotoSyncedAsync(string localId, string serverStoreId)
    {
        await InitializeAsync();
        try
        {
            await m_jsRuntime.InvokeVoidAsync("MotoRentOfflineDb.markPhotoSynced", localId, serverStoreId);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] MarkPhotoSynced error: {ex.Message}");
        }
    }

    #endregion

    #region Accident Report Operations

    /// <summary>
    /// Save an accident report (draft or submitted).
    /// </summary>
    public async Task<string> SaveAccidentReportAsync(OfflineAccidentReport report)
    {
        await InitializeAsync();
        try
        {
            var json = JsonSerializer.Serialize(report, m_jsonOptions);
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return await m_jsRuntime.InvokeAsync<string>("MotoRentOfflineDb.saveAccidentReport", element);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] SaveAccidentReport error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get accident reports for a rental.
    /// </summary>
    public async Task<List<OfflineAccidentReport>> GetAccidentReportsAsync(int rentalId)
    {
        await InitializeAsync();
        try
        {
            var result = await m_jsRuntime.InvokeAsync<JsonElement>("MotoRentOfflineDb.getAccidentReports", rentalId);
            if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
                return [];

            return JsonSerializer.Deserialize<List<OfflineAccidentReport>>(result.GetRawText(), m_jsonOptions) ?? [];
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] GetAccidentReports error: {ex.Message}");
            return [];
        }
    }

    #endregion

    #region Sync Queue Operations

    /// <summary>
    /// Add an item to the sync queue.
    /// </summary>
    public async Task AddToSyncQueueAsync(string type, object data)
    {
        await InitializeAsync();
        try
        {
            var json = JsonSerializer.Serialize(data, m_jsonOptions);
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            await m_jsRuntime.InvokeVoidAsync("MotoRentOfflineDb.addToSyncQueue", type, element);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] AddToSyncQueue error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get pending sync items.
    /// </summary>
    public async Task<List<OfflineSyncQueueItem>> GetPendingSyncItemsAsync()
    {
        await InitializeAsync();
        try
        {
            var result = await m_jsRuntime.InvokeAsync<JsonElement>("MotoRentOfflineDb.getPendingSyncItems");
            if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
                return [];

            return JsonSerializer.Deserialize<List<OfflineSyncQueueItem>>(result.GetRawText(), m_jsonOptions) ?? [];
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] GetPendingSyncItems error: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Remove an item from the sync queue.
    /// </summary>
    public async Task RemoveSyncItemAsync(int id)
    {
        await InitializeAsync();
        try
        {
            await m_jsRuntime.InvokeVoidAsync("MotoRentOfflineDb.removeSyncItem", id);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] RemoveSyncItem error: {ex.Message}");
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// Get storage usage estimate.
    /// </summary>
    public async Task<StorageEstimate?> GetStorageEstimateAsync()
    {
        await InitializeAsync();
        try
        {
            var result = await m_jsRuntime.InvokeAsync<JsonElement?>("MotoRentOfflineDb.getStorageEstimate");
            if (result == null || result.Value.ValueKind == JsonValueKind.Null)
                return null;

            return JsonSerializer.Deserialize<StorageEstimate>(result.Value.GetRawText(), m_jsonOptions);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] GetStorageEstimate error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generate a UUID.
    /// </summary>
    public async Task<string> GenerateUUIDAsync()
    {
        try
        {
            return await m_jsRuntime.InvokeAsync<string>("MotoRentOfflineDb.generateUUID");
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Delete the entire database (for testing/reset).
    /// </summary>
    public async Task DeleteDatabaseAsync()
    {
        try
        {
            await m_jsRuntime.InvokeVoidAsync("MotoRentOfflineDb.deleteDatabase");
            m_initialized = false;
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[IndexedDbService] DeleteDatabase error: {ex.Message}");
        }
    }

    #endregion

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

#region DTOs for Offline Storage

/// <summary>
/// Complete rental package for offline storage.
/// </summary>
public class OfflineRentalPackage
{
    public int RentalId { get; set; }
    public string? WebId { get; set; }
    public string? AccountNo { get; set; }
    public string Status { get; set; } = "Active";

    // Rental details
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset ExpectedEndDate { get; set; }
    public decimal RentalRate { get; set; }
    public decimal TotalAmount { get; set; }

    // Vehicle info
    public OfflineVehicleInfo? Vehicle { get; set; }

    // Renter info
    public OfflineRenterInfo? Renter { get; set; }

    // Shop info
    public OfflineShopInfo? Shop { get; set; }

    // Insurance
    public OfflineInsuranceInfo? Insurance { get; set; }

    // Deposit
    public OfflineDepositInfo? Deposit { get; set; }

    // Contract
    public string? ContractHtml { get; set; }
    public DateTimeOffset? ContractSignedAt { get; set; }

    // Emergency contacts
    public List<OfflineEmergencyContact> EmergencyContacts { get; set; } = [];

    // Timestamps
    public string? DownloadedAt { get; set; }
    public string? LastSyncAt { get; set; }
}

public class OfflineVehicleInfo
{
    public int VehicleId { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? LicensePlate { get; set; }
    public string? Color { get; set; }
    public string? ImageUrl { get; set; }
    public int MileageStart { get; set; }
}

public class OfflineRenterInfo
{
    public int RenterId { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class OfflineShopInfo
{
    public int ShopId { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? WhatsAppUrl { get; set; }
    public string? LineUrl { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public Dictionary<string, string> Hours { get; set; } = new();
    public bool IsCurrentlyOpen { get; set; }
}

public class OfflineInsuranceInfo
{
    public string? Name { get; set; }
    public decimal MaxCoverage { get; set; }
    public decimal Deductible { get; set; }
    public string? EmergencyNumber { get; set; }
}

public class OfflineDepositInfo
{
    public string? Type { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
}

public class OfflineEmergencyContact
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public int Priority { get; set; }
}

public class OfflinePendingPhoto
{
    public string? LocalId { get; set; }
    public int RentalId { get; set; }
    public string Type { get; set; } = "general"; // vehicle_condition, accident, document, general
    public string? Timestamp { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Accuracy { get; set; }
    public string? ImageDataBase64 { get; set; }
    public string? ThumbnailDataBase64 { get; set; }
    public bool Synced { get; set; }
    public string? SyncedAt { get; set; }
    public string? ServerStoreId { get; set; }
}

public class OfflineAccidentReport
{
    public string? LocalId { get; set; }
    public int RentalId { get; set; }
    public string Status { get; set; } = "draft"; // draft, submitted, synced

    // When/Where
    public string? AccidentDate { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationDescription { get; set; }

    // What happened
    public string? Description { get; set; }
    public string Severity { get; set; } = "minor"; // minor, moderate, severe

    // Damage
    public List<string> DamageAreas { get; set; } = [];
    public string? DamageDescription { get; set; }

    // Other parties
    public List<OfflineOtherParty> OtherParties { get; set; } = [];

    // Witnesses
    public List<OfflineWitness> Witnesses { get; set; } = [];

    // Police
    public bool PoliceInvolved { get; set; }
    public string? PoliceCaseNumber { get; set; }
    public string? PoliceStation { get; set; }

    // Photos
    public List<string> PhotoIds { get; set; } = [];

    // Timestamps
    public string? CreatedAt { get; set; }
    public string? SubmittedAt { get; set; }
    public string? SyncedAt { get; set; }
}

public class OfflineOtherParty
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? VehicleInfo { get; set; }
    public string? InsuranceInfo { get; set; }
}

public class OfflineWitness
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
}

public class OfflineSyncQueueItem
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public object? Data { get; set; }
    public string? Timestamp { get; set; }
    public int RetryCount { get; set; }
}

public class StorageEstimate
{
    public long Usage { get; set; }
    public long Quota { get; set; }
    public string? UsagePercent { get; set; }
}

#endregion
