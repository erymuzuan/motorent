-- TillTransaction table - Individual transactions in a till session
CREATE TABLE "TillTransaction"
(
    "TillTransactionId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "TillSessionId" INT GENERATED ALWAYS AS (("Json"->>'TillSessionId')::INT) STORED,
    "TransactionType" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'TransactionType')::VARCHAR(30)) STORED,
    "Direction" VARCHAR(10) GENERATED ALWAYS AS (("Json"->>'Direction')::VARCHAR(10)) STORED,
    "Amount" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'Amount')::NUMERIC(18,2)) STORED,
    "Category" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Category')::VARCHAR(50)) STORED,
    "PaymentId" INT GENERATED ALWAYS AS (("Json"->>'PaymentId')::INT) STORED,
    "DepositId" INT GENERATED ALWAYS AS (("Json"->>'DepositId')::INT) STORED,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "TransactionTime" TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'TransactionTime')) STORED,
    "IsVerified" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsVerified')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "TillTransaction" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_tilltransaction ON "TillTransaction" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_TillTransaction_TillSessionId ON "TillTransaction"("TillSessionId");
CREATE INDEX IX_TillTransaction_TransactionType ON "TillTransaction"("TransactionType");
CREATE INDEX IX_TillTransaction_TransactionTime ON "TillTransaction"("TransactionTime");
CREATE INDEX IX_TillTransaction_PaymentId ON "TillTransaction"("PaymentId");
CREATE INDEX IX_TillTransaction_RentalId ON "TillTransaction"("RentalId");
CREATE INDEX IX_TillTransaction_TenantId ON "TillTransaction"("tenant_id");
