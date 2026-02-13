-- DocumentTemplate table
CREATE TABLE "DocumentTemplate"
(
    "DocumentTemplateId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "WebId" VARCHAR(50) NULL,
    "Name" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(100)) STORED,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "Type" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Type')::VARCHAR(50)) STORED,
    "Status" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(50)) STORED,
    "IsDefault" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsDefault')::BOOLEAN) STORED,
    "Version" INT GENERATED ALWAYS AS (("Json"->>'Version')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "DocumentTemplate" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_documenttemplate ON "DocumentTemplate" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_DocumentTemplate_ShopId ON "DocumentTemplate"("ShopId");
CREATE INDEX IX_DocumentTemplate_Type ON "DocumentTemplate"("Type");
CREATE INDEX IX_DocumentTemplate_Status ON "DocumentTemplate"("Status");
CREATE INDEX IX_DocumentTemplate_TenantId ON "DocumentTemplate"("tenant_id");
