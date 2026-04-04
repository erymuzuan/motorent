-- TillSession table - Cashier till sessions for staff
CREATE TABLE "TillSession"
(
    "TillSessionId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "StaffUserName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'StaffUserName')::VARCHAR(100)) STORED,
    "Status" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(30)) STORED,
    "VerifiedByUserName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'VerifiedByUserName')::VARCHAR(100)) STORED,
    "ClosedByUserName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'ClosedByUserName')::VARCHAR(100)) STORED,
    "IsForceClose" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsForceClose')::BOOLEAN) STORED,
    "IsLateClose" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsLateClose')::BOOLEAN) STORED,
    "OpenedAt" TIMESTAMPTZ NULL,
    "ClosedAt" TIMESTAMPTZ NULL,
    "VerifiedAt" TIMESTAMPTZ NULL,
    "ExpectedCloseDate" DATE NULL,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "TillSession" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_tillsession ON "TillSession" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_TillSession_ShopId_Status ON "TillSession"("ShopId", "Status");
CREATE INDEX IX_TillSession_StaffUserName ON "TillSession"("StaffUserName");
CREATE INDEX IX_TillSession_TenantId ON "TillSession"("tenant_id");
