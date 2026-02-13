-- AssetLoan table - Loan/financing tracking
CREATE TABLE "AssetLoan"
(
    "AssetLoanId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT GENERATED ALWAYS AS (("Json"->>'AssetId')::INT) STORED,
    "LenderName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'LenderName')::VARCHAR(100)) STORED,
    "PrincipalAmount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'PrincipalAmount')::NUMERIC(12,2)) STORED,
    "AnnualInterestRate" NUMERIC(6,4) GENERATED ALWAYS AS (("Json"->>'AnnualInterestRate')::NUMERIC(6,4)) STORED,
    "TermMonths" INT GENERATED ALWAYS AS (("Json"->>'TermMonths')::INT) STORED,
    "MonthlyPayment" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'MonthlyPayment')::NUMERIC(12,2)) STORED,
    "StartDate" DATE NULL,
    "EndDate" DATE NULL,
    "RemainingPrincipal" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'RemainingPrincipal')::NUMERIC(12,2)) STORED,
    "PaymentsMade" INT GENERATED ALWAYS AS (("Json"->>'PaymentsMade')::INT) STORED,
    "NextPaymentDue" DATE NULL,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "AssetLoan" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_assetloan ON "AssetLoan" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_AssetLoan_AssetId ON "AssetLoan"("AssetId");
CREATE INDEX IX_AssetLoan_Status ON "AssetLoan"("Status");
CREATE INDEX IX_AssetLoan_NextPaymentDue ON "AssetLoan"("NextPaymentDue") WHERE "Status" = 'Active';
CREATE INDEX IX_AssetLoan_TenantId ON "AssetLoan"("tenant_id");
