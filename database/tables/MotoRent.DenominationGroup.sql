-- DenominationGroup table - Groups of denominations that share the same exchange rate
CREATE TABLE "DenominationGroup"
(
    "DenominationGroupId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Currency" CHAR(3) GENERATED ALWAYS AS (("Json"->>'Currency')::CHAR(3)) STORED,
    "GroupName" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'GroupName')::VARCHAR(50)) STORED,
    "SortOrder" INT GENERATED ALWAYS AS (("Json"->>'SortOrder')::INT) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "DenominationGroup" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_denominationgroup ON "DenominationGroup" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_DenominationGroup_Currency ON "DenominationGroup"("Currency", "IsActive", "SortOrder");
CREATE INDEX IX_DenominationGroup_TenantId ON "DenominationGroup"("tenant_id");
