-- Receipt table - Stores receipts for transactions
CREATE TABLE "Receipt"
(
    "ReceiptId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ReceiptNo" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'ReceiptNo')::VARCHAR(20)) STORED,
    "ReceiptType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'ReceiptType')::VARCHAR(20)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "BookingId" INT GENERATED ALWAYS AS (("Json"->>'BookingId')::INT) STORED,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "TillSessionId" INT GENERATED ALWAYS AS (("Json"->>'TillSessionId')::INT) STORED,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "RenterId" INT GENERATED ALWAYS AS (("Json"->>'RenterId')::INT) STORED,
    "CustomerName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'CustomerName')::VARCHAR(100)) STORED,
    "GrandTotal" NUMERIC(19,4) GENERATED ALWAYS AS (("Json"->>'GrandTotal')::NUMERIC(19,4)) STORED,
    "IssuedOn" TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'IssuedOn')) STORED,
    "IssuedByUserName" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'IssuedByUserName')::VARCHAR(50)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Receipt" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_receipt ON "Receipt" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE UNIQUE INDEX IX_Receipt_ReceiptNo ON "Receipt"("tenant_id", "ReceiptNo") WHERE "ReceiptNo" IS NOT NULL;
CREATE INDEX IX_Receipt_RentalId ON "Receipt"("RentalId");
CREATE INDEX IX_Receipt_BookingId ON "Receipt"("BookingId");
CREATE INDEX IX_Receipt_ShopId_IssuedOn ON "Receipt"("ShopId", "IssuedOn");
CREATE INDEX IX_Receipt_TillSessionId ON "Receipt"("TillSessionId");
CREATE INDEX IX_Receipt_RenterId ON "Receipt"("RenterId");
CREATE INDEX IX_Receipt_Status ON "Receipt"("Status");
CREATE INDEX IX_Receipt_TenantId ON "Receipt"("tenant_id");
