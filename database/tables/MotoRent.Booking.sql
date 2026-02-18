-- Booking table
-- Stores reservations/bookings made by customers
CREATE TABLE "Booking"
(
    "BookingId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "BookingRef" VARCHAR(10) GENERATED ALWAYS AS (("Json"->>'BookingRef')::VARCHAR(10)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "PreferredShopId" INT GENERATED ALWAYS AS (("Json"->>'PreferredShopId')::INT) STORED,
    "CheckedInAtShopId" INT GENERATED ALWAYS AS (("Json"->>'CheckedInAtShopId')::INT) STORED,
    "RenterId" INT GENERATED ALWAYS AS (("Json"->>'RenterId')::INT) STORED,
    "CustomerName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'CustomerName')::VARCHAR(100)) STORED,
    "CustomerPhone" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'CustomerPhone')::VARCHAR(50)) STORED,
    "CustomerEmail" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'CustomerEmail')::VARCHAR(100)) STORED,
    "StartDate" DATE NULL,
    "EndDate" DATE NULL,
    "PaymentStatus" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'PaymentStatus')::VARCHAR(20)) STORED,
    "TotalAmount" NUMERIC(19,4) GENERATED ALWAYS AS (("Json"->>'TotalAmount')::NUMERIC(19,4)) STORED,
    "AmountPaid" NUMERIC(19,4) GENERATED ALWAYS AS (("Json"->>'AmountPaid')::NUMERIC(19,4)) STORED,
    "BookingSource" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'BookingSource')::VARCHAR(20)) STORED,
    "AgentId" INT GENERATED ALWAYS AS (("Json"->>'AgentId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Booking" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_booking ON "Booking" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE UNIQUE INDEX IX_Booking_BookingRef ON "Booking"("tenant_id", "BookingRef");
CREATE INDEX IX_Booking_Status ON "Booking"("Status");
CREATE INDEX IX_Booking_PreferredShopId ON "Booking"("PreferredShopId");
CREATE INDEX IX_Booking_StartDate_Status ON "Booking"("StartDate", "Status");
CREATE INDEX IX_Booking_CustomerPhone ON "Booking"("CustomerPhone");
CREATE INDEX IX_Booking_CustomerEmail ON "Booking"("CustomerEmail");
CREATE INDEX IX_Booking_RenterId ON "Booking"("RenterId");
CREATE INDEX IX_Booking_AgentId ON "Booking"("AgentId");
CREATE INDEX IX_Booking_TenantId ON "Booking"("tenant_id");
