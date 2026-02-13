-- DamageReport table
CREATE TABLE "DamageReport"
(
    "DamageReportId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "MotorbikeId" INT GENERATED ALWAYS AS (("Json"->>'MotorbikeId')::INT) STORED,
    "Severity" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Severity')::VARCHAR(20)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "ReportedOn" TIMESTAMPTZ NULL,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "DamageReport" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_damagereport ON "DamageReport" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_DamageReport_RentalId ON "DamageReport"("RentalId");
CREATE INDEX IX_DamageReport_MotorbikeId ON "DamageReport"("MotorbikeId");
CREATE INDEX IX_DamageReport_TenantId ON "DamageReport"("tenant_id");
