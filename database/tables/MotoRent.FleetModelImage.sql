-- FleetModelImage table
CREATE TABLE "FleetModelImage"
(
    "FleetModelImageId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "FleetModelId" INT GENERATED ALWAYS AS (("Json"->>'FleetModelId')::INT) STORED,
    "IsPrimary" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsPrimary')::BOOLEAN) STORED,
    "DisplayOrder" INT GENERATED ALWAYS AS (("Json"->>'DisplayOrder')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "FleetModelImage" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_fleetmodelimage ON "FleetModelImage" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_FleetModelImage_FleetModelId ON "FleetModelImage"("FleetModelId");
CREATE INDEX IX_FleetModelImage_FleetModelId_IsPrimary ON "FleetModelImage"("FleetModelId", "IsPrimary");
CREATE INDEX IX_FleetModelImage_TenantId ON "FleetModelImage"("tenant_id");
