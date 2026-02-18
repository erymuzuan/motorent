-- Document table
CREATE TABLE "Document"
(
    "DocumentId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RenterId" INT GENERATED ALWAYS AS (("Json"->>'RenterId')::INT) STORED,
    "DocumentType" VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'DocumentType')::VARCHAR(50)) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Document" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_document ON "Document" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Document_RenterId ON "Document"("RenterId");
CREATE INDEX IX_Document_TenantId ON "Document"("tenant_id");
