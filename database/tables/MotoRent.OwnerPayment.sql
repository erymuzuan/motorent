-- OwnerPayment table - tracks payments due/paid to third-party vehicle owners
CREATE TABLE "OwnerPayment"
(
    "OwnerPaymentId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VehicleOwnerId" INT GENERATED ALWAYS AS (("Json"->>'VehicleOwnerId')::INT) STORED,
    "VehicleId" INT GENERATED ALWAYS AS (("Json"->>'VehicleId')::INT) STORED,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "PaymentModel" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'PaymentModel')::VARCHAR(20)) STORED,
    "Amount" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'Amount')::NUMERIC(10,2)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "RentalStartDate" TIMESTAMPTZ NULL,
    "RentalEndDate" TIMESTAMPTZ NULL,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "OwnerPayment" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_ownerpayment ON "OwnerPayment" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_OwnerPayment_VehicleOwnerId_Status ON "OwnerPayment"("VehicleOwnerId", "Status");
CREATE INDEX IX_OwnerPayment_RentalId ON "OwnerPayment"("RentalId");
CREATE INDEX IX_OwnerPayment_Status ON "OwnerPayment"("Status");
CREATE INDEX IX_OwnerPayment_RentalStartDate ON "OwnerPayment"("RentalStartDate");
CREATE INDEX IX_OwnerPayment_TenantId ON "OwnerPayment"("tenant_id");
