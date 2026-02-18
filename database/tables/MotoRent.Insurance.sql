-- Insurance table (organization-wide)
CREATE TABLE "Insurance"
(
    "InsuranceId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(100)) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Insurance" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_insurance ON "Insurance" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Insurance_IsActive ON "Insurance"("IsActive");
CREATE INDEX IX_Insurance_TenantId ON "Insurance"("tenant_id");
