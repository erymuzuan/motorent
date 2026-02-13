-- Comment Table (Tenant-specific)
CREATE TABLE "Comment"
(
    "CommentId"         INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Title"             VARCHAR(255) GENERATED ALWAYS AS (("Json"->>'Title')::VARCHAR(255)) STORED,
    "Type"              VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Type')::VARCHAR(50)) STORED,
    "ObjectWebId"       VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'ObjectWebId')::VARCHAR(50)) STORED,
    "User"              VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'User')::VARCHAR(100)) STORED,
    "EntityId"          INT GENERATED ALWAYS AS (("Json"->>'EntityId')::INT) STORED,
    "Hidden"            BOOLEAN GENERATED ALWAYS AS (("Json"->>'Hidden')::BOOLEAN) STORED,
    "Root"              INT GENERATED ALWAYS AS (("Json"->>'Root')::INT) STORED,
    "ReplyTo"           INT GENERATED ALWAYS AS (("Json"->>'ReplyTo')::INT) STORED,
    "SupportComment"    BOOLEAN GENERATED ALWAYS AS (("Json"->>'SupportComment')::BOOLEAN) STORED,
    "SupportStatus"     VARCHAR(25) GENERATED ALWAYS AS (("Json"->>'SupportStatus')::VARCHAR(25)) STORED,
    "Timestamp"         TIMESTAMPTZ NULL,
    "Json"              JSONB NOT NULL,
    "tenant_id"         VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Comment" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_comment ON "Comment" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_Comment_EntityId ON "Comment"("EntityId" DESC);
CREATE INDEX IX_Comment_Type ON "Comment"("Type", "Root");
CREATE INDEX IX_Comment_ObjectWebId ON "Comment"("ObjectWebId");
CREATE INDEX IX_Comment_TenantId ON "Comment"("tenant_id");
