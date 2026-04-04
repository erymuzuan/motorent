-- RateDelta table - Persisted delta adjustments for rate refresh reapplication
CREATE TABLE "RateDelta"
(
    "RateDeltaId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "Currency" CHAR(3) GENERATED ALWAYS AS (("Json"->>'Currency')::CHAR(3)) STORED,
    "DenominationGroupId" INT GENERATED ALWAYS AS (("Json"->>'DenominationGroupId')::INT) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "RateDelta" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_ratedelta ON "RateDelta" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_RateDelta_Shop ON "RateDelta"("ShopId", "Currency", "IsActive");
CREATE INDEX IX_RateDelta_Group ON "RateDelta"("DenominationGroupId", "IsActive");
CREATE INDEX IX_RateDelta_TenantId ON "RateDelta"("tenant_id");
