-- DailyClose table - Daily close records for shops
CREATE TABLE "DailyClose"
(
    "DailyCloseId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "ClosedByUserName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'ClosedByUserName')::VARCHAR(100)) STORED,
    "TotalVariance" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'TotalVariance')::NUMERIC(18,2)) STORED,
    "WasReopened" BOOLEAN GENERATED ALWAYS AS (("Json"->>'WasReopened')::BOOLEAN) STORED,
    "Date" DATE NOT NULL,
    "ClosedAt" TIMESTAMPTZ NULL,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "DailyClose" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_dailyclose ON "DailyClose" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE UNIQUE INDEX IX_DailyClose_ShopId_Date ON "DailyClose"("tenant_id", "ShopId", "Date");
CREATE INDEX IX_DailyClose_Status ON "DailyClose"("Status");
CREATE INDEX IX_DailyClose_ClosedAt ON "DailyClose"("ClosedAt");
CREATE INDEX IX_DailyClose_TenantId ON "DailyClose"("tenant_id");
