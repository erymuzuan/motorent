-- ServiceLocation table
-- Predefined pick-up/drop-off locations with associated fees
CREATE TABLE "ServiceLocation"
(
    "ServiceLocationId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "Name" VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(200)) STORED,
    "LocationType" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'LocationType')::VARCHAR(50)) STORED,
    "PickupFee" NUMERIC(18,2) GENERATED ALWAYS AS ((COALESCE("Json"->>'PickupFee', '0'))::NUMERIC(18,2)) STORED,
    "DropoffFee" NUMERIC(18,2) GENERATED ALWAYS AS ((COALESCE("Json"->>'DropoffFee', '0'))::NUMERIC(18,2)) STORED,
    "PickupAvailable" BOOLEAN GENERATED ALWAYS AS ((COALESCE("Json"->>'PickupAvailable', 'true'))::BOOLEAN) STORED,
    "DropoffAvailable" BOOLEAN GENERATED ALWAYS AS ((COALESCE("Json"->>'DropoffAvailable', 'true'))::BOOLEAN) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS ((COALESCE("Json"->>'IsActive', 'true'))::BOOLEAN) STORED,
    "DisplayOrder" INT GENERATED ALWAYS AS ((COALESCE("Json"->>'DisplayOrder', '0'))::INT) STORED,
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
ALTER TABLE "ServiceLocation" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_servicelocation ON "ServiceLocation" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_ServiceLocation_ShopId ON "ServiceLocation"("ShopId");
CREATE INDEX IX_ServiceLocation_ActiveType ON "ServiceLocation"("ShopId", "LocationType", "IsActive") WHERE "IsActive" = true;
CREATE INDEX IX_ServiceLocation_TenantId ON "ServiceLocation"("tenant_id");
