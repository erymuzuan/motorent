-- ShopSchedule table
-- Stores operating hours for specific dates
CREATE TABLE "ShopSchedule"
(
    "ShopScheduleId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "Date" DATE NULL,
    "IsOpen" BOOLEAN GENERATED ALWAYS AS ((COALESCE("Json"->>'IsOpen', 'true'))::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "ShopSchedule" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_shopschedule ON "ShopSchedule" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE UNIQUE INDEX IX_ShopSchedule_ShopDate ON "ShopSchedule"("tenant_id", "ShopId", "Date");
CREATE INDEX IX_ShopSchedule_DateRange ON "ShopSchedule"("ShopId", "Date");
CREATE INDEX IX_ShopSchedule_TenantId ON "ShopSchedule"("tenant_id");
