-- DepreciationEntry table - Period depreciation records
CREATE TABLE "DepreciationEntry"
(
    "DepreciationEntryId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT GENERATED ALWAYS AS (("Json"->>'AssetId')::INT) STORED,
    "PeriodStart" DATE NULL,
    "PeriodEnd" DATE NULL,
    "Amount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'Amount')::NUMERIC(12,2)) STORED,
    "BookValueStart" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'BookValueStart')::NUMERIC(12,2)) STORED,
    "BookValueEnd" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'BookValueEnd')::NUMERIC(12,2)) STORED,
    "Method" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'Method')::VARCHAR(30)) STORED,
    "EntryType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'EntryType')::VARCHAR(20)) STORED,
    "IsManualOverride" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsManualOverride')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "DepreciationEntry" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_depreciationentry ON "DepreciationEntry" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_DepreciationEntry_AssetId ON "DepreciationEntry"("AssetId");
CREATE INDEX IX_DepreciationEntry_Period ON "DepreciationEntry"("AssetId", "PeriodStart", "PeriodEnd");
CREATE INDEX IX_DepreciationEntry_Type ON "DepreciationEntry"("EntryType");
CREATE INDEX IX_DepreciationEntry_TenantId ON "DepreciationEntry"("tenant_id");
