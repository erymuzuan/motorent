-- VehicleImage table
CREATE TABLE "VehicleImage"
(
    "VehicleImageId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VehicleId" INT GENERATED ALWAYS AS (("Json"->>'VehicleId')::INT) STORED,
    "IsPrimary" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsPrimary')::BOOLEAN) STORED,
    "DisplayOrder" INT GENERATED ALWAYS AS (("Json"->>'DisplayOrder')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "VehicleImage" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_vehicleimage ON "VehicleImage" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_VehicleImage_VehicleId ON "VehicleImage"("VehicleId");
CREATE INDEX IX_VehicleImage_VehicleId_IsPrimary ON "VehicleImage"("VehicleId", "IsPrimary");
CREATE INDEX IX_VehicleImage_TenantId ON "VehicleImage"("tenant_id");
