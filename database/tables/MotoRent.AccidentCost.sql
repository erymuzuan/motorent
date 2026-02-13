-- AccidentCost table
CREATE TABLE "AccidentCost"
(
    "AccidentCostId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AccidentId" INT GENERATED ALWAYS AS (("Json"->>'AccidentId')::INT) STORED,
    "CostType" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'CostType')::VARCHAR(30)) STORED,
    "EstimatedAmount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'EstimatedAmount')::NUMERIC(12,2)) STORED,
    "ActualAmount" NUMERIC(12,2) GENERATED ALWAYS AS (("Json"->>'ActualAmount')::NUMERIC(12,2)) STORED,
    "IsCredit" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsCredit')::BOOLEAN) STORED,
    "IsApproved" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsApproved')::BOOLEAN) STORED,
    "PaidDate" DATE NULL,
    "AccidentPartyId" INT GENERATED ALWAYS AS (("Json"->>'AccidentPartyId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "AccidentCost" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_accidentcost ON "AccidentCost" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_AccidentCost_AccidentId ON "AccidentCost"("AccidentId");
CREATE INDEX IX_AccidentCost_CostType ON "AccidentCost"("AccidentId", "CostType");
CREATE INDEX IX_AccidentCost_TenantId ON "AccidentCost"("tenant_id");
