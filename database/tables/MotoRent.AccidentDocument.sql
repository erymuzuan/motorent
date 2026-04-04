-- AccidentDocument table
CREATE TABLE "AccidentDocument"
(
    "AccidentDocumentId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AccidentId" INT GENERATED ALWAYS AS (("Json"->>'AccidentId')::INT) STORED,
    "DocumentType" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'DocumentType')::VARCHAR(30)) STORED,
    "FileName" VARCHAR(255) GENERATED ALWAYS AS (("Json"->>'FileName')::VARCHAR(255)) STORED,
    "UploadedDate" DATE NULL,
    "AccidentPartyId" INT GENERATED ALWAYS AS (("Json"->>'AccidentPartyId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "AccidentDocument" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_accidentdocument ON "AccidentDocument" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_AccidentDocument_AccidentId ON "AccidentDocument"("AccidentId");
CREATE INDEX IX_AccidentDocument_DocumentType ON "AccidentDocument"("AccidentId", "DocumentType");
CREATE INDEX IX_AccidentDocument_TenantId ON "AccidentDocument"("tenant_id");
