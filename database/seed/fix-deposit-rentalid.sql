-- Fix deposits, payments, and rental names
-- Run this script with your tenant schema (e.g., AdamMoneyServiceCoLtd)

-- =====================================================
-- 1. Fix Deposits with RentalId = 0
-- =====================================================
PRINT '=== Checking Deposits ==='
SELECT d.DepositId, d.RentalId, d.CreatedTimestamp,
       r.RentalId as MatchingRentalId, r.CreatedTimestamp as RentalCreatedAt
FROM [AdamMoneyServiceCoLtd].[Deposit] d
LEFT JOIN [AdamMoneyServiceCoLtd].[Rental] r ON ABS(DATEDIFF(SECOND, d.CreatedTimestamp, r.CreatedTimestamp)) < 10
WHERE d.RentalId = 0 OR d.RentalId IS NULL

UPDATE d
SET d.[Json] = JSON_MODIFY(d.[Json], '$.RentalId', r.RentalId),
    d.ChangedTimestamp = SYSDATETIMEOFFSET(),
    d.ChangedBy = 'fix-script'
FROM [AdamMoneyServiceCoLtd].[Deposit] d
INNER JOIN [AdamMoneyServiceCoLtd].[Rental] r ON ABS(DATEDIFF(SECOND, d.CreatedTimestamp, r.CreatedTimestamp)) < 10
WHERE d.RentalId = 0 OR d.RentalId IS NULL

PRINT 'Fixed ' + CAST(@@ROWCOUNT AS VARCHAR) + ' deposits'

-- =====================================================
-- 2. Fix Payments with RentalId = 0
-- =====================================================
PRINT '=== Checking Payments ==='
SELECT p.PaymentId, p.RentalId, p.PaymentType, p.CreatedTimestamp,
       r.RentalId as MatchingRentalId, r.CreatedTimestamp as RentalCreatedAt
FROM [AdamMoneyServiceCoLtd].[Payment] p
LEFT JOIN [AdamMoneyServiceCoLtd].[Rental] r ON ABS(DATEDIFF(SECOND, p.CreatedTimestamp, r.CreatedTimestamp)) < 10
WHERE p.RentalId = 0 OR p.RentalId IS NULL

UPDATE p
SET p.[Json] = JSON_MODIFY(p.[Json], '$.RentalId', r.RentalId),
    p.ChangedTimestamp = SYSDATETIMEOFFSET(),
    p.ChangedBy = 'fix-script'
FROM [AdamMoneyServiceCoLtd].[Payment] p
INNER JOIN [AdamMoneyServiceCoLtd].[Rental] r ON ABS(DATEDIFF(SECOND, p.CreatedTimestamp, r.CreatedTimestamp)) < 10
WHERE p.RentalId = 0 OR p.RentalId IS NULL

PRINT 'Fixed ' + CAST(@@ROWCOUNT AS VARCHAR) + ' payments'

-- =====================================================
-- 3. Fix Rentals with missing RenterName
-- =====================================================
PRINT '=== Checking Rentals with missing RenterName ==='
SELECT r.RentalId, r.RenterId,
       JSON_VALUE(r.[Json], '$.RenterName') as CurrentRenterName,
       renter.FullName as RenterFullName
FROM [AdamMoneyServiceCoLtd].[Rental] r
INNER JOIN [AdamMoneyServiceCoLtd].[Renter] renter ON renter.RenterId = r.RenterId
WHERE JSON_VALUE(r.[Json], '$.RenterName') IS NULL

UPDATE r
SET r.[Json] = JSON_MODIFY(r.[Json], '$.RenterName', renter.FullName),
    r.ChangedTimestamp = SYSDATETIMEOFFSET(),
    r.ChangedBy = 'fix-script'
FROM [AdamMoneyServiceCoLtd].[Rental] r
INNER JOIN [AdamMoneyServiceCoLtd].[Renter] renter ON renter.RenterId = r.RenterId
WHERE JSON_VALUE(r.[Json], '$.RenterName') IS NULL

PRINT 'Fixed ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rental names'

-- =====================================================
-- 4. Verify
-- =====================================================
PRINT '=== Verification ==='
SELECT 'Deposits with RentalId=0' as Issue, COUNT(*) as [Count]
FROM [AdamMoneyServiceCoLtd].[Deposit] WHERE RentalId = 0 OR RentalId IS NULL
UNION ALL
SELECT 'Payments with RentalId=0', COUNT(*)
FROM [AdamMoneyServiceCoLtd].[Payment] WHERE RentalId = 0 OR RentalId IS NULL
UNION ALL
SELECT 'Rentals without RenterName', COUNT(*)
FROM [AdamMoneyServiceCoLtd].[Rental] r
INNER JOIN [AdamMoneyServiceCoLtd].[Renter] renter ON renter.RenterId = r.RenterId
WHERE JSON_VALUE(r.[Json], '$.RenterName') IS NULL
