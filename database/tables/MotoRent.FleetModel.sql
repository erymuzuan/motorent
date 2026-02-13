-- FleetModel table - shared vehicle attributes for vehicles of the same make/model/year
CREATE TABLE "FleetModel"
(
    "FleetModelId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VehicleModelId" INT GENERATED ALWAYS AS (("Json"->>'VehicleModelId')::INT) STORED,
    "Brand" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Brand')::VARCHAR(50)) STORED,
    "Model" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Model')::VARCHAR(50)) STORED,
    "Year" INT GENERATED ALWAYS AS (("Json"->>'Year')::INT) STORED,
    "VehicleType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'VehicleType')::VARCHAR(20)) STORED,
    "DailyRate" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'DailyRate')::NUMERIC(10,2)) STORED,
    "DurationType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'DurationType')::VARCHAR(20)) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "FleetModel" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_fleetmodel ON "FleetModel" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_FleetModel_VehicleType_IsActive ON "FleetModel"("VehicleType", "IsActive");
CREATE INDEX IX_FleetModel_Brand_Model ON "FleetModel"("Brand", "Model");
CREATE INDEX IX_FleetModel_TenantId ON "FleetModel"("tenant_id");
