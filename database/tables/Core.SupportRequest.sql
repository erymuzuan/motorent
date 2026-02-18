-- SupportRequest table (Core - no tenant isolation)
-- Support tickets created from comments
CREATE TABLE "SupportRequest"
(
    "SupportRequestId"  INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "No"                VARCHAR(15) GENERATED ALWAYS AS (("Json"->>'No')::VARCHAR(15)) STORED,
    "Title"             VARCHAR(255) GENERATED ALWAYS AS (("Json"->>'Title')::VARCHAR(255)) STORED,
    "AccountNo"         VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'AccountNo')::VARCHAR(50)) STORED,
    "Status"            VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(50)) STORED,
    "Priority"          VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Priority')::VARCHAR(50)) STORED,
    "AssignedTo"        VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'AssignedTo')::VARCHAR(100)) STORED,
    "CommentId"         INT GENERATED ALWAYS AS (("Json"->>'CommentId')::INT) STORED,
    "Type"              VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Type')::VARCHAR(50)) STORED,
    "ObjectWebId"       VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'ObjectWebId')::VARCHAR(50)) STORED,
    "RequestedBy"       VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'RequestedBy')::VARCHAR(100)) STORED,
    "EntityId"          INT GENERATED ALWAYS AS (("Json"->>'EntityId')::INT) STORED,
    "TotalMinutesResponded" DOUBLE PRECISION GENERATED ALWAYS AS (("Json"->>'TotalMinutesResponded')::DOUBLE PRECISION) STORED,
    "TotalMinutesResolved"  DOUBLE PRECISION GENERATED ALWAYS AS (("Json"->>'TotalMinutesResolved')::DOUBLE PRECISION) STORED,
    "Timestamp"         TIMESTAMPTZ NULL,
    "ResolvedTimestamp"  TIMESTAMPTZ NULL,
    "ClosedTimestamp"    TIMESTAMPTZ NULL,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_SupportRequest_EntityId ON "SupportRequest"("EntityId" DESC);

CREATE INDEX IX_SupportRequest_Type_AccountNo ON "SupportRequest"("Type", "AccountNo", "CommentId");

CREATE INDEX IX_SupportRequest_No ON "SupportRequest"("No", "AccountNo");

CREATE INDEX IX_SupportRequest_Status ON "SupportRequest"("Status", "Priority");
