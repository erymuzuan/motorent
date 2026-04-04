-- AssetExpense table - Expense tracking for assets
CREATE TABLE "AssetExpense"
(
    "AssetExpenseId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AssetId" INT GENERATED ALWAYS AS (("Json"->>'AssetId')::INT) STORED,
    "Category" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'Category')::VARCHAR(30)) STORED,
    "Amount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'Amount')::NUMERIC(12,2)) STORED,
    "ExpenseDate" DATE NULL,
    "IsPaid" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsPaid')::BOOLEAN) STORED,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "AccidentId" INT GENERATED ALWAYS AS (("Json"->>'AccidentId')::INT) STORED,
    "MaintenanceScheduleId" INT GENERATED ALWAYS AS (("Json"->>'MaintenanceScheduleId')::INT) STORED,
    "AssetLoanPaymentId" INT GENERATED ALWAYS AS (("Json"->>'AssetLoanPaymentId')::INT) STORED,
    "AccountingPeriod" CHAR(7) GENERATED ALWAYS AS (("Json"->>'AccountingPeriod')::CHAR(7)) STORED,
    "IsTaxDeductible" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsTaxDeductible')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "AssetExpense" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_assetexpense ON "AssetExpense" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_AssetExpense_AssetId ON "AssetExpense"("AssetId");
CREATE INDEX IX_AssetExpense_Category ON "AssetExpense"("AssetId", "Category");
CREATE INDEX IX_AssetExpense_ExpenseDate ON "AssetExpense"("ExpenseDate");
CREATE INDEX IX_AssetExpense_AccountingPeriod ON "AssetExpense"("AccountingPeriod");
CREATE INDEX IX_AssetExpense_TenantId ON "AssetExpense"("tenant_id");
