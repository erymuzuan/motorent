-- MaintenanceSchedule table - Per-motorbike maintenance tracking
CREATE TABLE "MaintenanceSchedule"
(
    "MaintenanceScheduleId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "MotorbikeId" INT GENERATED ALWAYS AS (("Json"->>'MotorbikeId')::INT) STORED,
    "ServiceTypeId" INT GENERATED ALWAYS AS (("Json"->>'ServiceTypeId')::INT) STORED,
    "LastServiceDate" DATE NULL,
    "LastServiceMileage" INT GENERATED ALWAYS AS (("Json"->>'LastServiceMileage')::INT) STORED,
    "NextDueDate" DATE NULL,
    "NextDueMileage" INT GENERATED ALWAYS AS (("Json"->>'NextDueMileage')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "MaintenanceSchedule" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_maintenanceschedule ON "MaintenanceSchedule" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_MaintenanceSchedule_MotorbikeId ON "MaintenanceSchedule"("MotorbikeId");
CREATE INDEX IX_MaintenanceSchedule_ServiceTypeId ON "MaintenanceSchedule"("ServiceTypeId");
CREATE INDEX IX_MaintenanceSchedule_NextDueDate ON "MaintenanceSchedule"("NextDueDate");
CREATE INDEX IX_MaintenanceSchedule_Composite ON "MaintenanceSchedule"("MotorbikeId", "ServiceTypeId");
CREATE INDEX IX_MaintenanceSchedule_TenantId ON "MaintenanceSchedule"("tenant_id");
