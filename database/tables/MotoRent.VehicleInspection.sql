-- VehicleInspection table for 3D damage marking and inspection records
CREATE TABLE "VehicleInspection"
(
    "VehicleInspectionId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VehicleId" INT GENERATED ALWAYS AS (("Json"->>'VehicleId')::INT) STORED,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "MaintenanceRecordId" INT GENERATED ALWAYS AS (("Json"->>'MaintenanceRecordId')::INT) STORED,
    "AccidentId" INT GENERATED ALWAYS AS (("Json"->>'AccidentId')::INT) STORED,
    "InspectionType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'InspectionType')::VARCHAR(20)) STORED,
    "OverallCondition" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'OverallCondition')::VARCHAR(20)) STORED,
    "InspectedAt" TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'InspectedAt')) STORED,
    "OdometerReading" INT GENERATED ALWAYS AS (("Json"->>'OdometerReading')::INT) STORED,
    "FuelLevel" INT GENERATED ALWAYS AS (("Json"->>'FuelLevel')::INT) STORED,
    "PreviousInspectionId" INT GENERATED ALWAYS AS (("Json"->>'PreviousInspectionId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "VehicleInspection" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_vehicleinspection ON "VehicleInspection" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_VehicleInspection_VehicleId ON "VehicleInspection"("VehicleId");
CREATE INDEX IX_VehicleInspection_RentalId ON "VehicleInspection"("RentalId");
CREATE INDEX IX_VehicleInspection_MaintenanceRecordId ON "VehicleInspection"("MaintenanceRecordId");
CREATE INDEX IX_VehicleInspection_AccidentId ON "VehicleInspection"("AccidentId");
CREATE INDEX IX_VehicleInspection_Type_Date ON "VehicleInspection"("InspectionType", "InspectedAt");
CREATE INDEX IX_VehicleInspection_TenantId ON "VehicleInspection"("tenant_id");
