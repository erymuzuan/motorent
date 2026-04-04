-- Deposit table
CREATE TABLE "Deposit"
(
    "DepositId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "DepositType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'DepositType')::VARCHAR(20)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Deposit" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_deposit ON "Deposit" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Deposit_RentalId ON "Deposit"("RentalId");
CREATE INDEX IX_Deposit_TenantId ON "Deposit"("tenant_id");
