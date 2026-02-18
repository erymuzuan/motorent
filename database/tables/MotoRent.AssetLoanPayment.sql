-- AssetLoanPayment table - Individual loan payment records
CREATE TABLE "AssetLoanPayment"
(
    "AssetLoanPaymentId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetLoanId" INT GENERATED ALWAYS AS (("Json"->>'AssetLoanId')::INT) STORED,
    "PaymentNumber" INT GENERATED ALWAYS AS (("Json"->>'PaymentNumber')::INT) STORED,
    "DueDate" DATE NULL,
    "PaidDate" DATE NULL,
    "TotalAmount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'TotalAmount')::NUMERIC(12,2)) STORED,
    "PrincipalAmount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'PrincipalAmount')::NUMERIC(12,2)) STORED,
    "InterestAmount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'InterestAmount')::NUMERIC(12,2)) STORED,
    "BalanceAfter" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'BalanceAfter')::NUMERIC(12,2)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "AssetLoanPayment" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_assetloanpayment ON "AssetLoanPayment" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_AssetLoanPayment_LoanId ON "AssetLoanPayment"("AssetLoanId");
CREATE INDEX IX_AssetLoanPayment_Status ON "AssetLoanPayment"("AssetLoanId", "Status");
CREATE INDEX IX_AssetLoanPayment_DueDate ON "AssetLoanPayment"("DueDate");
CREATE INDEX IX_AssetLoanPayment_TenantId ON "AssetLoanPayment"("tenant_id");
