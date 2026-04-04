-- Motorbike table (organization-wide)
CREATE TABLE "Motorbike"
(
    "MotorbikeId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "LicensePlate" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'LicensePlate')::VARCHAR(20)) STORED,
    "Brand" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Brand')::VARCHAR(50)) STORED,
    "Model" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Model')::VARCHAR(50)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "DailyRate" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'DailyRate')::NUMERIC(10,2)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Motorbike" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_motorbike ON "Motorbike" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Motorbike_Status ON "Motorbike"("Status");
CREATE INDEX IX_Motorbike_TenantId ON "Motorbike"("tenant_id");
