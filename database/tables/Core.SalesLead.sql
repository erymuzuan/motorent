-- SalesLead table (Core - no tenant isolation)
-- Tracks leads from contact forms and other sources
CREATE TABLE "SalesLead"
(
    "SalesLeadId"       INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "No"                VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'No')::VARCHAR(20)) STORED,
    "Name"              VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(200)) STORED,
    "Email"             VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Email')::VARCHAR(200)) STORED,
    "Phone"             VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Phone')::VARCHAR(50)) STORED,
    "Company"           VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Company')::VARCHAR(200)) STORED,
    "Status"            VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Status')::VARCHAR(20)) STORED,
    "Source"            VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Source')::VARCHAR(20)) STORED,
    "PlanInterested"    VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'PlanInterested')::VARCHAR(20)) STORED,
    "FleetSize"         INT GENERATED ALWAYS AS (("Json"->>'FleetSize')::INT) STORED,
    "OrganizationId"    INT GENERATED ALWAYS AS (("Json"->>'OrganizationId')::INT) STORED,
    "AccountNo"         VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'AccountNo')::VARCHAR(20)) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IX_SalesLead_No ON "SalesLead"("No");

CREATE INDEX IX_SalesLead_Email ON "SalesLead"("Email");

CREATE INDEX IX_SalesLead_Status ON "SalesLead"("Status", "Source", "CreatedTimestamp");
