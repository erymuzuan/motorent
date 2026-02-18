-- ServiceType table - Configurable maintenance service types (organization-wide)
CREATE TABLE "ServiceType"
(
    "ServiceTypeId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(100)) STORED,
    "DaysInterval" INT GENERATED ALWAYS AS (("Json"->>'DaysInterval')::INT) STORED,
    "KmInterval" INT GENERATED ALWAYS AS (("Json"->>'KmInterval')::INT) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "SortOrder" INT GENERATED ALWAYS AS (("Json"->>'SortOrder')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "ServiceType" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_servicetype ON "ServiceType" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_ServiceType_IsActive ON "ServiceType"("IsActive");
CREATE INDEX IX_ServiceType_TenantId ON "ServiceType"("tenant_id");
