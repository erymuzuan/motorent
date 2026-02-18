-- Agent table
-- Stores agents (tour guides, hotels, travel agencies) who make bookings on behalf of customers
CREATE TABLE "Agent"
(
    "AgentId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AgentCode" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'AgentCode')::VARCHAR(50)) STORED,
    "Name" VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(200)) STORED,
    "AgentType" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'AgentType')::VARCHAR(50)) STORED,
    "Status" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "Phone" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Phone')::VARCHAR(50)) STORED,
    "Email" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Email')::VARCHAR(100)) STORED,
    "CommissionType" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'CommissionType')::VARCHAR(50)) STORED,
    "CommissionRate" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'CommissionRate')::NUMERIC(18,2)) STORED,
    "CommissionBalance" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'CommissionBalance')::NUMERIC(18,2)) STORED,
    "TotalBookings" INT GENERATED ALWAYS AS (("Json"->>'TotalBookings')::INT) STORED,
    "TotalCommissionEarned" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'TotalCommissionEarned')::NUMERIC(18,2)) STORED,
    "TotalCommissionPaid" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'TotalCommissionPaid')::NUMERIC(18,2)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Agent" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_agent ON "Agent" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE UNIQUE INDEX IX_Agent_AgentCode ON "Agent"("tenant_id", "AgentCode");
CREATE INDEX IX_Agent_Status ON "Agent"("Status");
CREATE INDEX IX_Agent_AgentType ON "Agent"("AgentType");
CREATE INDEX IX_Agent_AgentType_Status ON "Agent"("AgentType", "Status");
CREATE INDEX IX_Agent_CommissionBalance ON "Agent"("CommissionBalance") WHERE "Status" = 'Active';
CREATE INDEX IX_Agent_TenantId ON "Agent"("tenant_id");
