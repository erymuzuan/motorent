-- Accident table (organization-wide)
CREATE TABLE "Accident"
(
    "AccidentId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VehicleId" INT GENERATED ALWAYS AS (("Json"->>'VehicleId')::INT) STORED,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "ReferenceNo" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'ReferenceNo')::VARCHAR(50)) STORED,
    "Severity" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Severity')::VARCHAR(20)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "PoliceInvolved" BOOLEAN GENERATED ALWAYS AS (("Json"->>'PoliceInvolved')::BOOLEAN) STORED,
    "InsuranceClaimFiled" BOOLEAN GENERATED ALWAYS AS (("Json"->>'InsuranceClaimFiled')::BOOLEAN) STORED,
    "AccidentDate" DATE NULL,
    "ReportedDate" DATE NULL,
    "ResolvedDate" DATE NULL,
    -- Financial summary columns
    "TotalEstimatedCost" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'TotalEstimatedCost')::NUMERIC(12,2)) STORED,
    "TotalActualCost" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'TotalActualCost')::NUMERIC(12,2)) STORED,
    "ReserveAmount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'ReserveAmount')::NUMERIC(12,2)) STORED,
    "InsurancePayoutReceived" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'InsurancePayoutReceived')::NUMERIC(12,2)) STORED,
    "NetCost" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'NetCost')::NUMERIC(12,2)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL,
    "ChangedBy" VARCHAR(50) NOT NULL,
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL,
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL
);
ALTER TABLE "Accident" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_accident ON "Accident" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Accident_Status ON "Accident"("Status");
CREATE INDEX IX_Accident_VehicleId ON "Accident"("VehicleId");
CREATE INDEX IX_Accident_RentalId ON "Accident"("RentalId");
CREATE INDEX IX_Accident_AccidentDate ON "Accident"("AccidentDate");
CREATE INDEX IX_Accident_Severity_Status ON "Accident"("Severity", "Status");
CREATE UNIQUE INDEX IX_Accident_ReferenceNo ON "Accident"("tenant_id", "ReferenceNo");
CREATE INDEX IX_Accident_TenantId ON "Accident"("tenant_id");
