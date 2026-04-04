-- MaintenanceRecord table - Detailed service history with attachments
CREATE TABLE "MaintenanceRecord"
(
    "MaintenanceRecordId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VehicleId" INT GENERATED ALWAYS AS (("Json"->>'VehicleId')::INT) STORED,
    "ServiceTypeId" INT GENERATED ALWAYS AS (("Json"->>'ServiceTypeId')::INT) STORED,
    "ServiceTypeName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'ServiceTypeName')::VARCHAR(100)) STORED,
    "ServiceDate" DATE NULL,
    "ServiceMileage" INT GENERATED ALWAYS AS (("Json"->>'ServiceMileage')::INT) STORED,
    "Cost" NUMERIC(19,4) GENERATED ALWAYS AS (("Json"->>'Cost')::NUMERIC(19,4)) STORED,
    "WorkshopName" VARCHAR(200) GENERATED ALWAYS AS (("Json"->'Workshop'->>'Name')::VARCHAR(200)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "MaintenanceRecord" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_maintenancerecord ON "MaintenanceRecord" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_MaintenanceRecord_VehicleId ON "MaintenanceRecord"("VehicleId");
CREATE INDEX IX_MaintenanceRecord_ServiceTypeId ON "MaintenanceRecord"("ServiceTypeId");
CREATE INDEX IX_MaintenanceRecord_ServiceDate ON "MaintenanceRecord"("ServiceDate");
CREATE INDEX IX_MaintenanceRecord_Composite ON "MaintenanceRecord"("VehicleId", "ServiceDate");
CREATE INDEX IX_MaintenanceRecord_TenantId ON "MaintenanceRecord"("tenant_id");
