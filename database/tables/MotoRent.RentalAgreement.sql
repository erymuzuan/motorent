-- RentalAgreement table
CREATE TABLE "RentalAgreement"
(
    "RentalAgreementId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RentalId" INT GENERATED ALWAYS AS (("Json"->>'RentalId')::INT) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "RentalAgreement" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_rentalagreement ON "RentalAgreement" USING ("tenant_id" = current_setting('app.current_tenant'));

CREATE INDEX IX_RentalAgreement_RentalId ON "RentalAgreement"("RentalId");
CREATE INDEX IX_RentalAgreement_TenantId ON "RentalAgreement"("tenant_id");
