-- DenominationRate table - Exchange rates by denomination group
CREATE TABLE "DenominationRate"
(
    "DenominationRateId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "Currency" CHAR(3) GENERATED ALWAYS AS (("Json"->>'Currency')::CHAR(3)) STORED,
    "DenominationGroupId" INT GENERATED ALWAYS AS (("Json"->>'DenominationGroupId')::INT) STORED,
    "ProviderCode" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'ProviderCode')::VARCHAR(20)) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "EffectiveDate" TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'EffectiveDate')) STORED,
    "ExpiresOn" TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'ExpiresOn')) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "DenominationRate" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_denominationrate ON "DenominationRate" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_DenominationRate_Currency ON "DenominationRate"("Currency", "IsActive");
CREATE INDEX IX_DenominationRate_Shop ON "DenominationRate"("ShopId", "Currency", "IsActive");
CREATE INDEX IX_DenominationRate_Group ON "DenominationRate"("DenominationGroupId", "IsActive");
CREATE INDEX IX_DenominationRate_EffectiveDate ON "DenominationRate"("EffectiveDate");
CREATE INDEX IX_DenominationRate_TenantId ON "DenominationRate"("tenant_id");
