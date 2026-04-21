-- PricingRule table - Dynamic pricing rules (organization-wide)
CREATE TABLE "PricingRule"
(
    "PricingRuleId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(100)) STORED,
    "RuleType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'RuleType')::VARCHAR(20)) STORED,
    "StartDate" DATE GENERATED ALWAYS AS (immutable_text_to_date("Json"->>'StartDate')) STORED,
    "EndDate" DATE GENERATED ALWAYS AS (immutable_text_to_date("Json"->>'EndDate')) STORED,
    "IsRecurring" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsRecurring')::BOOLEAN) STORED,
    "Multiplier" NUMERIC(5,2) GENERATED ALWAYS AS (("Json"->>'Multiplier')::NUMERIC(5,2)) STORED,
    "Priority" INT GENERATED ALWAYS AS (("Json"->>'Priority')::INT) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "VehicleType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'VehicleType')::VARCHAR(20)) STORED,
    "VehicleId" INT GENERATED ALWAYS AS (("Json"->>'VehicleId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "PricingRule" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_pricingrule ON "PricingRule" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_PricingRule_DateRange ON "PricingRule"("StartDate", "EndDate", "IsActive");
CREATE INDEX IX_PricingRule_TenantId ON "PricingRule"("tenant_id");
