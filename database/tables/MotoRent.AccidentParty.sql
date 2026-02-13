-- AccidentParty table
CREATE TABLE "AccidentParty"
(
    "AccidentPartyId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AccidentId" INT GENERATED ALWAYS AS (("Json"->>'AccidentId')::INT) STORED,
    "PartyType" VARCHAR(30) GENERATED ALWAYS AS (("Json"->>'PartyType')::VARCHAR(30)) STORED,
    "Name" VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(100)) STORED,
    "IsInjured" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsInjured')::BOOLEAN) STORED,
    "IsAtFault" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsAtFault')::BOOLEAN) STORED,
    "RenterId" INT GENERATED ALWAYS AS (("Json"->>'RenterId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "AccidentParty" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_accidentparty ON "AccidentParty" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_AccidentParty_AccidentId ON "AccidentParty"("AccidentId");
CREATE INDEX IX_AccidentParty_PartyType ON "AccidentParty"("AccidentId", "PartyType");
CREATE INDEX IX_AccidentParty_TenantId ON "AccidentParty"("tenant_id");
