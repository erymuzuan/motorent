-- Shop table
CREATE TABLE "Shop"
(
    "ShopId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(200)) STORED,
    "Location" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Location')::VARCHAR(100)) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    -- GPS coordinates for map display
    "Latitude" DOUBLE PRECISION GENERATED ALWAYS AS (("Json"->'GpsLocation'->>'Lat')::DOUBLE PRECISION) STORED,
    "Longitude" DOUBLE PRECISION GENERATED ALWAYS AS (("Json"->'GpsLocation'->>'Lng')::DOUBLE PRECISION) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Shop" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_shop ON "Shop" USING ("tenant_id" = current_setting('app.current_tenant'));
CREATE INDEX IX_Shop_TenantId ON "Shop"("tenant_id");
