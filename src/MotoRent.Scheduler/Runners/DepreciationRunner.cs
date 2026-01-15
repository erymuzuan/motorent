using Microsoft.Extensions.Logging;
using MotoRent.Services;

namespace MotoRent.Scheduler.Runners;

/// <summary>
/// Scheduled task that runs monthly depreciation for all active assets.
/// This runner should be executed at the end of each month to calculate
/// and record depreciation entries for all vehicles with asset records.
/// </summary>
public class DepreciationRunner : ITaskRunner
{
    private readonly AssetService m_assetService;
    private readonly ILogger<DepreciationRunner> m_logger;

    public string Name => nameof(DepreciationRunner);
    public string Description => "Runs monthly depreciation calculations for all active assets";

    public DepreciationRunner(
        AssetService assetService,
        ILogger<DepreciationRunner> logger)
    {
        m_assetService = assetService;
        m_logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        m_logger.LogInformation("Starting monthly depreciation run...");

        var periodEnd = DateTimeOffset.Now;

        try
        {
            // Get count of assets needing depreciation
            var needingDepreciation = await m_assetService.GetAssetsNeedingDepreciationCountAsync();
            m_logger.LogInformation("Found {Count} assets needing depreciation", needingDepreciation);

            if (needingDepreciation == 0)
            {
                m_logger.LogInformation("No assets require depreciation this period");
                return;
            }

            // Run batch depreciation
            var result = await m_assetService.RunMonthlyDepreciationAsync(periodEnd, "scheduler");

            // Log results
            m_logger.LogInformation(
                "Depreciation run completed: Processed={Processed}, Skipped={Skipped}, FullyDepreciated={FullyDepreciated}, Failed={Failed}",
                result.Processed,
                result.Skipped,
                result.FullyDepreciated,
                result.Failed);

            // Log any errors
            foreach (var error in result.Errors)
            {
                m_logger.LogWarning("Depreciation error: {Error}", error);
            }

            if (result.Failed > 0)
            {
                m_logger.LogWarning(
                    "Depreciation run completed with {FailedCount} failures. Check errors above for details.",
                    result.Failed);
            }
            else
            {
                m_logger.LogInformation(
                    "Depreciation run completed successfully. {ProcessedCount} assets depreciated.",
                    result.Processed);
            }
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Fatal error during depreciation run");
            throw;
        }
    }
}
