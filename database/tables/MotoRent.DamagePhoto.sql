-- DamagePhoto table
CREATE TABLE "DamagePhoto"
(
    "DamagePhotoId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DamageReportId" INT GENERATED ALWAYS AS (("Json"->>'DamageReportId')::INT) STORED,
    "PhotoType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'PhotoType')::VARCHAR(20)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "DamagePhoto" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_damagephoto ON "DamagePhoto" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_DamagePhoto_DamageReportId ON "DamagePhoto"("DamageReportId");
CREATE INDEX IX_DamagePhoto_TenantId ON "DamagePhoto"("tenant_id");
