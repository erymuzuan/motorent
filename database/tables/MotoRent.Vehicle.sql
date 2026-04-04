-- Vehicle table - replaces Motorbike with support for multiple vehicle types
CREATE TABLE "Vehicle"
(
    "VehicleId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- Fleet Model
    "FleetModelId" INT GENERATED ALWAYS AS (("Json"->>'FleetModelId')::INT) STORED,
    -- Location and Pool
    "HomeShopId" INT GENERATED ALWAYS AS (("Json"->>'HomeShopId')::INT) STORED,
    "VehiclePoolId" INT GENERATED ALWAYS AS (("Json"->>'VehiclePoolId')::INT) STORED,
    "CurrentShopId" INT GENERATED ALWAYS AS (("Json"->>'CurrentShopId')::INT) STORED,
    -- Vehicle Type and Classification
    "VehicleType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'VehicleType')::VARCHAR(20)) STORED,
    "Segment" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Segment')::VARCHAR(20)) STORED,
    "DurationType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'DurationType')::VARCHAR(20)) STORED,
    -- Common Properties
    "LicensePlate" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'LicensePlate')::VARCHAR(20)) STORED,
    "Brand" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Brand')::VARCHAR(50)) STORED,
    "Model" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Model')::VARCHAR(50)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    -- Pricing
    "DailyRate" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'DailyRate')::NUMERIC(10,2)) STORED,
    "Rate15Min" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'Rate15Min')::NUMERIC(10,2)) STORED,
    "Rate30Min" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'Rate30Min')::NUMERIC(10,2)) STORED,
    "Rate1Hour" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'Rate1Hour')::NUMERIC(10,2)) STORED,
    -- Driver/Guide Fees
    "DriverDailyFee" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'DriverDailyFee')::NUMERIC(10,2)) STORED,
    "GuideDailyFee" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'GuideDailyFee')::NUMERIC(10,2)) STORED,
    -- Third-Party Owner
    "VehicleOwnerId" INT GENERATED ALWAYS AS (("Json"->>'VehicleOwnerId')::INT) STORED,
    "OwnerPaymentModel" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'OwnerPaymentModel')::VARCHAR(20)) STORED,
    "OwnerDailyRate" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'OwnerDailyRate')::NUMERIC(10,2)) STORED,
    "OwnerRevenueSharePercent" NUMERIC(5,4) GENERATED ALWAYS AS (("Json"->>'OwnerRevenueSharePercent')::NUMERIC(5,4)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Vehicle" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_vehicle ON "Vehicle" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Vehicle_HomeShopId_Status ON "Vehicle"("HomeShopId", "Status");
CREATE INDEX IX_Vehicle_CurrentShopId_Status ON "Vehicle"("CurrentShopId", "Status");
CREATE INDEX IX_Vehicle_VehiclePoolId_Status ON "Vehicle"("VehiclePoolId", "Status");
CREATE INDEX IX_Vehicle_VehicleType_Status ON "Vehicle"("VehicleType", "Status");
CREATE UNIQUE INDEX IX_Vehicle_LicensePlate ON "Vehicle"("tenant_id", "LicensePlate");
CREATE INDEX IX_Vehicle_VehicleOwnerId ON "Vehicle"("VehicleOwnerId");
CREATE INDEX IX_Vehicle_FleetModelId ON "Vehicle"("FleetModelId");
CREATE INDEX IX_Vehicle_TenantId ON "Vehicle"("tenant_id");
