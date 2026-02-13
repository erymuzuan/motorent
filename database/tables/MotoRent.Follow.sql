-- Follow Table (Tenant-specific)
CREATE TABLE "Follow"
(
    "FollowId"          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Type"              VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Type')::VARCHAR(50)) STORED,
    "User"              VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'User')::VARCHAR(100)) STORED,
    "IsActive"          BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "EntityId"          INT GENERATED ALWAYS AS (("Json"->>'EntityId')::INT) STORED,
    "Json"              JSONB NOT NULL,
    "tenant_id"         VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Follow" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_follow ON "Follow" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Follow_EntityId_User_Type ON "Follow"("EntityId", "User", "Type");
CREATE INDEX IX_Follow_TenantId ON "Follow"("tenant_id");
