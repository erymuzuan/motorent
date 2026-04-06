CREATE TABLE "AiUsageLog"
(
    "AiUsageLogId"      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "UserName"          VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'UserName')::VARCHAR(200)) STORED,
    "AccountNo"         VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'AccountNo')::VARCHAR(50)) STORED,
    "IpAddress"         VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'IpAddress')::VARCHAR(50)) STORED,
    "SessionId"         VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'SessionId')::VARCHAR(50)) STORED,
    "ServiceName"       VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'ServiceName')::VARCHAR(50)) STORED,
    "Model"             VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Model')::VARCHAR(100)) STORED,
    "Success"           BOOLEAN GENERATED ALWAYS AS (("Json"->>'Success')::BOOLEAN) STORED,
    "DateTime"          TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'DateTime')) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_AiUsageLog_DateTime ON "AiUsageLog"("DateTime");
CREATE INDEX IX_AiUsageLog_UserName_DateTime ON "AiUsageLog"("UserName", "DateTime");
CREATE INDEX IX_AiUsageLog_IpAddress_DateTime ON "AiUsageLog"("IpAddress", "DateTime");
CREATE INDEX IX_AiUsageLog_ServiceName ON "AiUsageLog"("ServiceName");
