-- AgentCommission table
-- Tracks commission for agent bookings
CREATE TABLE "AgentCommission"
(
    "AgentCommissionId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AgentId" INT GENERATED ALWAYS AS (("Json"->>'AgentId')::INT) STORED,
    "BookingId" INT GENERATED ALWAYS AS (("Json"->>'BookingId')::INT) STORED,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "CommissionAmount" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'CommissionAmount')::NUMERIC(18,2)) STORED,
    "BookingTotal" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'BookingTotal')::NUMERIC(18,2)) STORED,
    "EligibleDate" TIMESTAMPTZ NULL,
    "ApprovedDate" TIMESTAMPTZ NULL,
    "PaidDate" TIMESTAMPTZ NULL,
    "AgentCode" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'AgentCode')::VARCHAR(50)) STORED,
    "BookingRef" VARCHAR(10) GENERATED ALWAYS AS (("Json"->>'BookingRef')::VARCHAR(10)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "AgentCommission" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_agentcommission ON "AgentCommission" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_AgentCommission_AgentId ON "AgentCommission"("AgentId");
CREATE INDEX IX_AgentCommission_BookingId ON "AgentCommission"("BookingId");
CREATE INDEX IX_AgentCommission_RentalId ON "AgentCommission"("RentalId");
CREATE INDEX IX_AgentCommission_Status ON "AgentCommission"("Status");
CREATE INDEX IX_AgentCommission_AgentId_Status ON "AgentCommission"("AgentId", "Status");
CREATE INDEX IX_AgentCommission_TenantId ON "AgentCommission"("tenant_id");
