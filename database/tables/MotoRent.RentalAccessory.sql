-- RentalAccessory table
CREATE TABLE "RentalAccessory"
(
    "RentalAccessoryId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "AccessoryId" INT GENERATED ALWAYS AS (("Json"->>'AccessoryId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "RentalAccessory" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_rentalaccessory ON "RentalAccessory" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_RentalAccessory_RentalId ON "RentalAccessory"("RentalId");
CREATE INDEX IX_RentalAccessory_TenantId ON "RentalAccessory"("tenant_id");
