-- ============================================
-- Fix TillSession #4 - Add missing deposit refund TotalCashOut
-- Issue: Deposit refunds were not recorded as Cash Out transactions
-- Date: 2026-02-28
-- ============================================

-- Before: TotalCashOut = 0, ExpectedCash = 4100
-- After:  TotalCashOut = 1500, ExpectedCash = 2600 (3 deposits × 500 refunded)

-- ==================== SQL Server ====================

BEGIN TRANSACTION;

-- Update TillSession #4 TotalCashOut and ExpectedCash
UPDATE [AdamMotoGolok].[TillSession]
SET [Json] = JSON_MODIFY(JSON_MODIFY([Json], '$.TotalCashOut', 1500), '$.ExpectedCash', 2600),
    [ChangedBy] = 'system',
    [ChangedTimestamp] = GETUTCDATE()
WHERE [TillSessionId] = 4;

-- Verify the update
SELECT [TillSessionId],
       CAST(JSON_VALUE([Json], '$.OpeningFloat') AS DECIMAL(18,2)) AS opening_float,
       CAST(JSON_VALUE([Json], '$.TotalCashIn') AS DECIMAL(18,2)) AS cash_in,
       CAST(JSON_VALUE([Json], '$.TotalCashOut') AS DECIMAL(18,2)) AS cash_out,
       CAST(JSON_VALUE([Json], '$.ExpectedCash') AS DECIMAL(18,2)) AS expected_cash
FROM [AdamMotoGolok].[TillSession]
WHERE [TillSessionId] = 4;

COMMIT TRANSACTION;
