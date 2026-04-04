-- AccidentNote table
CREATE TABLE "AccidentNote"
(
    "AccidentNoteId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AccidentId" INT GENERATED ALWAYS AS (("Json"->>'AccidentId')::INT) STORED,
    "NoteType" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'NoteType')::VARCHAR(30)) STORED,
    "IsPinned" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsPinned')::BOOLEAN) STORED,
    "IsInternal" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsInternal')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "AccidentNote" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_accidentnote ON "AccidentNote" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_AccidentNote_AccidentId ON "AccidentNote"("AccidentId");
CREATE INDEX IX_AccidentNote_TenantId ON "AccidentNote"("tenant_id");
