-- Payment table
CREATE TABLE "Payment"
(
    "PaymentId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "PaymentType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'PaymentType')::VARCHAR(20)) STORED,
    "PaymentMethod" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'PaymentMethod')::VARCHAR(20)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "PaidOn" TIMESTAMPTZ NULL,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Payment" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_payment ON "Payment" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Payment_RentalId ON "Payment"("RentalId");
CREATE INDEX IX_Payment_TenantId ON "Payment"("tenant_id");
