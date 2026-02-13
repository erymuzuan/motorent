-- VehicleOwner table - third-party vehicle owners
CREATE TABLE "VehicleOwner"
(
    "VehicleOwnerId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(100)) STORED,
    "Phone" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Phone')::VARCHAR(20)) STORED,
    "Email" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Email')::VARCHAR(100)) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "VehicleOwner" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_vehicleowner ON "VehicleOwner" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_VehicleOwner_Name ON "VehicleOwner"("Name");
CREATE INDEX IX_VehicleOwner_IsActive ON "VehicleOwner"("IsActive");
CREATE INDEX IX_VehicleOwner_TenantId ON "VehicleOwner"("tenant_id");
