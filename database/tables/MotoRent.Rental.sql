-- Rental table
CREATE TABLE "Rental"
(
    "RentalId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- Shop and Location (COALESCE for backward compatibility with old ShopId)
    "RentedFromShopId" INT GENERATED ALWAYS AS ((COALESCE("Json"->>'RentedFromShopId', "Json"->>'ShopId'))::INT) STORED,
    "ReturnedToShopId" INT GENERATED ALWAYS AS (("Json"->>'ReturnedToShopId')::INT) STORED,
    "VehiclePoolId" INT GENERATED ALWAYS AS (("Json"->>'VehiclePoolId')::INT) STORED,
    -- Renter and Vehicle (COALESCE for backward compatibility with old MotorbikeId)
    "RenterId" INT GENERATED ALWAYS AS (("Json"->>'RenterId')::INT) STORED,
    "VehicleId" INT GENERATED ALWAYS AS ((COALESCE("Json"->>'VehicleId', "Json"->>'MotorbikeId'))::INT) STORED,
    -- Duration Type (defaults to Daily for backward compatibility)
    "DurationType" VARCHAR(20) GENERATED ALWAYS AS ((COALESCE("Json"->>'DurationType', 'Daily'))::VARCHAR(20)) STORED,
    "IntervalMinutes" INT GENERATED ALWAYS AS (("Json"->>'IntervalMinutes')::INT) STORED,
    -- Booking Reference (for rentals created from bookings)
    "BookingId" INT GENERATED ALWAYS AS (("Json"->>'BookingId')::INT) STORED,
    -- Status and Dates
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "StartDate" DATE NULL,
    "ExpectedEndDate" DATE NULL,
    -- Driver/Guide
    "IncludeDriver" BOOLEAN GENERATED ALWAYS AS ((COALESCE("Json"->>'IncludeDriver', 'false'))::BOOLEAN) STORED,
    "IncludeGuide" BOOLEAN GENERATED ALWAYS AS ((COALESCE("Json"->>'IncludeGuide', 'false'))::BOOLEAN) STORED,
    -- Till Session
    "TillSessionId" INT GENERATED ALWAYS AS (("Json"->>'TillSessionId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Rental" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_rental ON "Rental" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Rental_RentedFromShopId_Status ON "Rental"("RentedFromShopId", "Status");
CREATE INDEX IX_Rental_ReturnedToShopId ON "Rental"("ReturnedToShopId") WHERE "ReturnedToShopId" IS NOT NULL;
CREATE INDEX IX_Rental_VehiclePoolId ON "Rental"("VehiclePoolId") WHERE "VehiclePoolId" IS NOT NULL;
CREATE INDEX IX_Rental_RenterId ON "Rental"("RenterId");
CREATE INDEX IX_Rental_VehicleId ON "Rental"("VehicleId");
CREATE INDEX IX_Rental_DurationType ON "Rental"("DurationType");
CREATE INDEX IX_Rental_BookingId ON "Rental"("BookingId") WHERE "BookingId" IS NOT NULL;
CREATE INDEX IX_Rental_TillSessionId ON "Rental"("TillSessionId") WHERE "TillSessionId" IS NOT NULL;
CREATE INDEX IX_Rental_TenantId ON "Rental"("tenant_id");
