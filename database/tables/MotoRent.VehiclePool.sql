-- VehiclePool table - groups shops that share vehicle inventory
CREATE TABLE "VehiclePool"
(
    "VehiclePoolId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(200)) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "PrimaryShopId" INT GENERATED ALWAYS AS (("Json"->>'PrimaryShopId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "VehiclePool" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_vehiclepool ON "VehiclePool" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_VehiclePool_IsActive ON "VehiclePool"("IsActive");
CREATE INDEX IX_VehiclePool_TenantId ON "VehiclePool"("tenant_id");
