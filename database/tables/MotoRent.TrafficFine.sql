-- Traffic/parking fines issued against company vehicles
CREATE TABLE "TrafficFine"
(
    "TrafficFineId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VehicleId" INT GENERATED ALWAYS AS (("Json"->>'VehicleId')::INT) STORED,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "FineType" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'FineType')::VARCHAR(30)) STORED,
    "Status" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(30)) STORED,
    "Amount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'Amount')::NUMERIC(12,2)) STORED,
    "ReferenceNo" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'ReferenceNo')::VARCHAR(50)) STORED,
    "FineDate" DATE NULL,
    "ResolvedDate" DATE NULL,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL,
    "ChangedBy" VARCHAR(50) NOT NULL,
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL,
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL
);
ALTER TABLE "TrafficFine" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_trafficfine ON "TrafficFine" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_TrafficFine_VehicleId ON "TrafficFine"("VehicleId");
CREATE INDEX IX_TrafficFine_RentalId ON "TrafficFine"("RentalId");
CREATE INDEX IX_TrafficFine_Status_FineDate ON "TrafficFine"("Status", "FineDate");
CREATE INDEX IX_TrafficFine_FineDate ON "TrafficFine"("FineDate");
CREATE INDEX IX_TrafficFine_TenantId ON "TrafficFine"("tenant_id");
