$schema = "KrabiBeachRentals"
$server = "localhost\DEV2022"
$database = "MotoRent"

Write-Host "Provisioning schema: $schema" -ForegroundColor Cyan

# Create schema if not exists
$createSchema = @"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '$schema')
BEGIN
    EXEC('CREATE SCHEMA [$schema]')
    PRINT 'Schema created: $schema'
END
ELSE
BEGIN
    PRINT 'Schema already exists: $schema'
END
"@

try {
    Invoke-Sqlcmd -ServerInstance $server -Database $database -Query $createSchema -ErrorAction Stop
    Write-Host "  Schema check complete" -ForegroundColor Green
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
    exit 1
}

# Get all table definition files
$tableFiles = @(
    "MotoRent.Shop.sql",
    "MotoRent.Renter.sql",
    "MotoRent.Document.sql",
    "MotoRent.Vehicle.sql",
    "MotoRent.VehiclePool.sql",
    "MotoRent.VehicleOwner.sql",
    "MotoRent.VehicleImage.sql",
    "MotoRent.Insurance.sql",
    "MotoRent.Accessory.sql",
    "MotoRent.Booking.sql",
    "MotoRent.Rental.sql",
    "MotoRent.RentalAccessory.sql",
    "MotoRent.Payment.sql",
    "MotoRent.Deposit.sql",
    "MotoRent.Receipt.sql",
    "MotoRent.DamageReport.sql",
    "MotoRent.DamagePhoto.sql",
    "MotoRent.TillSession.sql",
    "MotoRent.TillTransaction.sql",
    "MotoRent.TillDenominationCount.sql",
    "MotoRent.DailyClose.sql",
    "MotoRent.ShortageLog.sql",
    "MotoRent.ExchangeRate.sql",
    "MotoRent.Agent.sql",
    "MotoRent.AgentCommission.sql",
    "MotoRent.AgentInvoice.sql",
    "MotoRent.MaintenanceSchedule.sql",
    "MotoRent.MaintenanceRecord.sql",
    "MotoRent.ServiceType.sql",
    "MotoRent.ServiceLocation.sql",
    "MotoRent.ShopSchedule.sql",
    "MotoRent.PricingRule.sql",
    "MotoRent.OwnerPayment.sql",
    "MotoRent.Accident.sql",
    "MotoRent.AccidentCost.sql",
    "MotoRent.AccidentDocument.sql",
    "MotoRent.AccidentNote.sql",
    "MotoRent.AccidentParty.sql",
    "MotoRent.Comment.sql",
    "MotoRent.Follow.sql",
    "MotoRent.Asset.sql",
    "MotoRent.AssetExpense.sql",
    "MotoRent.AssetLoan.sql",
    "MotoRent.AssetLoanPayment.sql",
    "MotoRent.DepreciationEntry.sql",
    "MotoRent.RentalAgreement.sql"
)

$tablesDir = Join-Path $PSScriptRoot "..\tables"

foreach ($file in $tableFiles) {
    $filePath = Join-Path $tablesDir $file
    if (Test-Path $filePath) {
        $tableName = $file -replace "MotoRent\.", "" -replace "\.sql", ""

        # Check if table already exists
        $checkTable = "SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('$schema') AND name = '$tableName'"
        $exists = Invoke-Sqlcmd -ServerInstance $server -Database $database -Query $checkTable -ErrorAction SilentlyContinue

        if ($exists) {
            Write-Host "  Table already exists: $tableName" -ForegroundColor Yellow
            continue
        }

        Write-Host "  Creating table: $tableName" -ForegroundColor Gray

        # Read and modify SQL
        $sql = Get-Content $filePath -Raw
        $sql = $sql -replace '\[<schema>\]', "[$schema]"
        $sql = $sql -replace '<schema>', $schema

        # Split by GO and execute each batch
        $batches = $sql -split '\r?\nGO\r?\n' | Where-Object { $_.Trim() -ne "" }

        foreach ($batch in $batches) {
            if ($batch.Trim() -ne "" -and $batch.Trim() -ne "GO") {
                try {
                    Invoke-Sqlcmd -ServerInstance $server -Database $database -Query $batch -ErrorAction Stop
                } catch {
                    Write-Host "    Error in batch: $_" -ForegroundColor Red
                }
            }
        }
        Write-Host "    Done" -ForegroundColor Green
    } else {
        Write-Host "  File not found: $file" -ForegroundColor Yellow
    }
}

# Seed data for Shop
Write-Host "`nSeeding data..." -ForegroundColor Cyan

$seedShop = @"
IF NOT EXISTS (SELECT 1 FROM [$schema].[Shop] WHERE [Name] = 'Krabi Beach Shop')
BEGIN
    INSERT INTO [$schema].[Shop] ([Json])
    VALUES (N'{
        "ShopId": 0,
        "Name": "Krabi Beach Shop",
        "Location": "Krabi",
        "Address": "123 Beach Road, Ao Nang, Krabi 81000, Thailand",
        "Phone": "+66 75 123 4567",
        "Email": "info@krabibeachrentals.com",
        "IsActive": true,
        "GpsLocation": {"Lat": 8.0308, "Lng": 98.8263}
    }')
    PRINT 'Shop created: Krabi Beach Shop'
END
"@

try {
    Invoke-Sqlcmd -ServerInstance $server -Database $database -Query $seedShop -ErrorAction Stop
    Write-Host "  Shop seeded" -ForegroundColor Green
} catch {
    Write-Host "  Error seeding shop: $_" -ForegroundColor Red
}

# Get ShopId
$shopIdQuery = "SELECT [ShopId] FROM [$schema].[Shop] WHERE [Name] = 'Krabi Beach Shop'"
$shopResult = Invoke-Sqlcmd -ServerInstance $server -Database $database -Query $shopIdQuery
$shopId = $shopResult.ShopId

Write-Host "  Shop ID: $shopId" -ForegroundColor Gray

# Seed Vehicles
$seedVehicles = @"
IF NOT EXISTS (SELECT 1 FROM [$schema].[Vehicle] WHERE [LicensePlate] = 'กข 1234')
BEGIN
    INSERT INTO [$schema].[Vehicle] ([Json])
    VALUES
    (N'{"HomeShopId": $shopId, "CurrentShopId": $shopId, "LicensePlate": "กข 1234", "Brand": "Honda", "Model": "Click 125", "VehicleType": "Motorbike", "DurationType": "Daily", "Status": "Available", "DailyRate": 250, "EngineCC": 125, "Color": "White", "Year": 2024}'),
    (N'{"HomeShopId": $shopId, "CurrentShopId": $shopId, "LicensePlate": "กข 2345", "Brand": "Honda", "Model": "Click 125", "VehicleType": "Motorbike", "DurationType": "Daily", "Status": "Available", "DailyRate": 250, "EngineCC": 125, "Color": "Red", "Year": 2024}'),
    (N'{"HomeShopId": $shopId, "CurrentShopId": $shopId, "LicensePlate": "กข 3456", "Brand": "Honda", "Model": "PCX 160", "VehicleType": "Motorbike", "DurationType": "Daily", "Status": "Available", "DailyRate": 400, "EngineCC": 160, "Color": "Black", "Year": 2024}'),
    (N'{"HomeShopId": $shopId, "CurrentShopId": $shopId, "LicensePlate": "กข 4567", "Brand": "Yamaha", "Model": "NMAX 155", "VehicleType": "Motorbike", "DurationType": "Daily", "Status": "Available", "DailyRate": 450, "EngineCC": 155, "Color": "Blue", "Year": 2024}'),
    (N'{"HomeShopId": $shopId, "CurrentShopId": $shopId, "LicensePlate": "กข 5678", "Brand": "Yamaha", "Model": "Aerox 155", "VehicleType": "Motorbike", "DurationType": "Daily", "Status": "Available", "DailyRate": 500, "EngineCC": 155, "Color": "Yellow", "Year": 2024}'),
    (N'{"HomeShopId": $shopId, "CurrentShopId": $shopId, "LicensePlate": "กข 6789", "Brand": "Honda", "Model": "Wave 110", "VehicleType": "Motorbike", "DurationType": "Daily", "Status": "Available", "DailyRate": 200, "EngineCC": 110, "Color": "Black", "Year": 2023}'),
    (N'{"HomeShopId": $shopId, "CurrentShopId": $shopId, "LicensePlate": "กข 7890", "Brand": "Honda", "Model": "Scoopy", "VehicleType": "Motorbike", "DurationType": "Daily", "Status": "Maintenance", "DailyRate": 220, "EngineCC": 110, "Color": "Pink", "Year": 2023}')
    PRINT 'Vehicles created'
END
"@

try {
    Invoke-Sqlcmd -ServerInstance $server -Database $database -Query $seedVehicles -ErrorAction Stop
    Write-Host "  Vehicles seeded" -ForegroundColor Green
} catch {
    Write-Host "  Error seeding vehicles: $_" -ForegroundColor Red
}

# Seed Insurance
$seedInsurance = @"
IF NOT EXISTS (SELECT 1 FROM [$schema].[Insurance] WHERE [Name] = 'Basic')
BEGIN
    INSERT INTO [$schema].[Insurance] ([Json])
    VALUES
    (N'{"ShopId": $shopId, "Name": "Basic", "Description": "Basic coverage for minor damages", "DailyRate": 50, "MaxCoverage": 5000, "Deductible": 1000, "IsActive": true}'),
    (N'{"ShopId": $shopId, "Name": "Premium", "Description": "Full coverage including theft", "DailyRate": 150, "MaxCoverage": 20000, "Deductible": 500, "IsActive": true}'),
    (N'{"ShopId": $shopId, "Name": "Full Coverage", "Description": "Complete protection with zero deductible", "DailyRate": 250, "MaxCoverage": 50000, "Deductible": 0, "IsActive": true}')
    PRINT 'Insurance packages created'
END
"@

try {
    Invoke-Sqlcmd -ServerInstance $server -Database $database -Query $seedInsurance -ErrorAction Stop
    Write-Host "  Insurance seeded" -ForegroundColor Green
} catch {
    Write-Host "  Error seeding insurance: $_" -ForegroundColor Red
}

# Seed Accessories
$seedAccessories = @"
IF NOT EXISTS (SELECT 1 FROM [$schema].[Accessory] WHERE [Name] = 'Helmet (Full Face)')
BEGIN
    INSERT INTO [$schema].[Accessory] ([Json])
    VALUES
    (N'{"ShopId": $shopId, "Name": "Helmet (Full Face)", "DailyRate": 0, "QuantityAvailable": 20, "IsIncluded": true, "IsActive": true}'),
    (N'{"ShopId": $shopId, "Name": "Helmet (Half Face)", "DailyRate": 0, "QuantityAvailable": 30, "IsIncluded": true, "IsActive": true}'),
    (N'{"ShopId": $shopId, "Name": "Phone Holder", "DailyRate": 20, "QuantityAvailable": 15, "IsIncluded": false, "IsActive": true}'),
    (N'{"ShopId": $shopId, "Name": "Rain Poncho", "DailyRate": 30, "QuantityAvailable": 25, "IsIncluded": false, "IsActive": true}'),
    (N'{"ShopId": $shopId, "Name": "Luggage Box", "DailyRate": 50, "QuantityAvailable": 10, "IsIncluded": false, "IsActive": true}')
    PRINT 'Accessories created'
END
"@

try {
    Invoke-Sqlcmd -ServerInstance $server -Database $database -Query $seedAccessories -ErrorAction Stop
    Write-Host "  Accessories seeded" -ForegroundColor Green
} catch {
    Write-Host "  Error seeding accessories: $_" -ForegroundColor Red
}

Write-Host "`nProvisioning complete for $schema!" -ForegroundColor Cyan
