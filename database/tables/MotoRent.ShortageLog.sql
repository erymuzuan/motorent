-- ShortageLog table - Variance accountability records
CREATE TABLE "ShortageLog"
(
    "ShortageLogId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ShopId" INT GENERATED ALWAYS AS (("Json"->>'ShopId')::INT) STORED,
    "TillSessionId" INT GENERATED ALWAYS AS (("Json"->>'TillSessionId')::INT) STORED,
    "DailyCloseId" INT GENERATED ALWAYS AS (("Json"->>'DailyCloseId')::INT) STORED,
    "StaffUserName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'StaffUserName')::VARCHAR(100)) STORED,
    "Currency" CHAR(3) GENERATED ALWAYS AS (("Json"->>'Currency')::CHAR(3)) STORED,
    "Amount" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'Amount')::NUMERIC(18,2)) STORED,
    "AmountInThb" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'AmountInThb')::NUMERIC(18,2)) STORED,
    "LoggedAt" TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'LoggedAt')) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "ShortageLog" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_shortagelog ON "ShortageLog" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_ShortageLog_ShopId_TillSessionId ON "ShortageLog"("ShopId", "TillSessionId");
CREATE INDEX IX_ShortageLog_StaffUserName ON "ShortageLog"("StaffUserName");
CREATE INDEX IX_ShortageLog_LoggedAt ON "ShortageLog"("LoggedAt");
CREATE INDEX IX_ShortageLog_TenantId ON "ShortageLog"("tenant_id");
