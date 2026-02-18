-- Feedback table (Core - no tenant isolation)
CREATE TABLE "Feedback"
(
    "FeedbackId"       INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AccountNo"        VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'AccountNo')::VARCHAR(50)) STORED,
    "UserName"         VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'UserName')::VARCHAR(200)) STORED,
    "Type"             VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Type')::VARCHAR(20)) STORED,
    "Status"           VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "Url"              VARCHAR(500) GENERATED ALWAYS AS (("Json"->>'Url')::VARCHAR(500)) STORED,
    "LogEntryId"       INT GENERATED ALWAYS AS (("Json"->>'LogEntryId')::INT) STORED,
    "Timestamp"        TIMESTAMPTZ NULL,
    "Json"             JSONB NOT NULL,
    "CreatedBy"        VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"        VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IDX_Core_Feedback_AccountStatus
    ON "Feedback"("AccountNo", "Status");

CREATE INDEX IDX_Core_Feedback_StatusTimestamp
    ON "Feedback"("Status", "Timestamp");

CREATE INDEX IDX_Core_Feedback_LogEntryId
    ON "Feedback"("LogEntryId");
