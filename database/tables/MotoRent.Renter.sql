-- Renter table
CREATE TABLE "Renter"
(
    "RenterId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "FullName" VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'FullName')::VARCHAR(200)) STORED,
    "Phone" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Phone')::VARCHAR(50)) STORED,
    "PassportNo" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'PassportNo')::VARCHAR(50)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Renter" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_renter ON "Renter" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Renter_PassportNo ON "Renter"("PassportNo", "FullName");
CREATE INDEX IX_Renter_TenantId ON "Renter"("tenant_id");
