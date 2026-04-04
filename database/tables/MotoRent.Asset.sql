-- Asset table - Financial tracking record for vehicles
CREATE TABLE "Asset"
(
    "AssetId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VehicleId" INT GENERATED ALWAYS AS (("Json"->>'VehicleId')::INT) STORED,
    "AcquisitionDate" DATE NULL,
    "AcquisitionCost" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'AcquisitionCost')::NUMERIC(12,2)) STORED,
    "FirstRentalDate" DATE NULL,
    "IsPreExisting" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsPreExisting')::BOOLEAN) STORED,
    "DepreciationMethod" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'DepreciationMethod')::VARCHAR(30)) STORED,
    "UsefulLifeMonths" INT GENERATED ALWAYS AS (("Json"->>'UsefulLifeMonths')::INT) STORED,
    "ResidualValue" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'ResidualValue')::NUMERIC(12,2)) STORED,
    "CurrentBookValue" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'CurrentBookValue')::NUMERIC(12,2)) STORED,
    "AccumulatedDepreciation" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'AccumulatedDepreciation')::NUMERIC(12,2)) STORED,
    "TotalExpenses" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'TotalExpenses')::NUMERIC(12,2)) STORED,
    "TotalRevenue" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'TotalRevenue')::NUMERIC(12,2)) STORED,
    "LastDepreciationDate" DATE NULL,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "DisposalDate" DATE NULL,
    "AssetLoanId" INT GENERATED ALWAYS AS (("Json"->>'AssetLoanId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Asset" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_asset ON "Asset" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE UNIQUE INDEX IX_Asset_VehicleId ON "Asset"("tenant_id", "VehicleId");
CREATE INDEX IX_Asset_Status ON "Asset"("Status");
CREATE INDEX IX_Asset_AcquisitionDate ON "Asset"("AcquisitionDate");
CREATE INDEX IX_Asset_DepreciationMethod ON "Asset"("DepreciationMethod");
CREATE INDEX IX_Asset_TenantId ON "Asset"("tenant_id");
