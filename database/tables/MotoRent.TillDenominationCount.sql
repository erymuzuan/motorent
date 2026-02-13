-- TillDenominationCount table - Denomination-level cash counts for till sessions
CREATE TABLE "TillDenominationCount"
(
    "TillDenominationCountId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "TillSessionId" INT GENERATED ALWAYS AS (("Json"->>'TillSessionId')::INT) STORED,
    "CountType" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'CountType')::VARCHAR(20)) STORED,
    "CountedAt" TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'CountedAt')) STORED,
    "CountedByUserName" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'CountedByUserName')::VARCHAR(100)) STORED,
    "TotalInThb" NUMERIC(18,2) GENERATED ALWAYS AS (("Json"->>'TotalInThb')::NUMERIC(18,2)) STORED,
    "IsFinal" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsFinal')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "TillDenominationCount" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_tilldenominationcount ON "TillDenominationCount" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_TillDenominationCount_Session_Type ON "TillDenominationCount"("TillSessionId", "CountType");
CREATE INDEX IX_TillDenominationCount_CountedBy ON "TillDenominationCount"("CountedByUserName");
CREATE INDEX IX_TillDenominationCount_CountedAt ON "TillDenominationCount"("CountedAt");
CREATE INDEX IX_TillDenominationCount_TenantId ON "TillDenominationCount"("tenant_id");
