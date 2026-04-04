-- Accessory table
CREATE TABLE "Accessory"
(
    "AccessoryId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "Name" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(100)) STORED,
    "QuantityAvailable" INT GENERATED ALWAYS AS (("Json"->>'QuantityAvailable')::INT) STORED,
    "DailyRate" NUMERIC(10,2) GENERATED ALWAYS AS (("Json"->>'DailyRate')::NUMERIC(10,2)) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "IsIncluded" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsIncluded')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Accessory" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_accessory ON "Accessory" USING ("tenant_id" = current_setting('app.current_tenant'));
CREATE INDEX IX_Accessory_ShopId ON "Accessory"("ShopId");
CREATE INDEX IX_Accessory_TenantId ON "Accessory"("tenant_id");
