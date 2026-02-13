-- ExchangeRate table - Exchange rates for multi-currency cash payments
CREATE TABLE "ExchangeRate"
(
    "ExchangeRateId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Currency" CHAR(3) GENERATED ALWAYS AS (("Json"->>'Currency')::CHAR(3)) STORED,
    "BuyRate" NUMERIC(18,4) GENERATED ALWAYS AS (("Json"->>'BuyRate')::NUMERIC(18,4)) STORED,
    "Source" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Source')::VARCHAR(20)) STORED,
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
ALTER TABLE "ExchangeRate" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_exchangerate ON "ExchangeRate" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_ExchangeRate_Currency_Active ON "ExchangeRate"("Currency", "IsActive");
CREATE INDEX IX_ExchangeRate_EffectiveDate ON "ExchangeRate"("EffectiveDate");
CREATE INDEX IX_ExchangeRate_TenantId ON "ExchangeRate"("tenant_id");
