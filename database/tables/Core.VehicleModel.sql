-- VehicleModel table (Core - no tenant isolation)
-- Global vehicle make/model lookup data (shared across all tenants)
CREATE TABLE "VehicleModel"
(
    "VehicleModelId"    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Make"              VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Make')::VARCHAR(100)) STORED,
    "Model"             VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Model')::VARCHAR(100)) STORED,
    "VehicleType"       VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'VehicleType')::VARCHAR(20)) STORED,
    "Segment"           VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Segment')::VARCHAR(20)) STORED,
    "EngineCC"          INT GENERATED ALWAYS AS (("Json"->>'EngineCC')::INT) STORED,
    "IsActive"          BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "DisplayOrder"      INT GENERATED ALWAYS AS (("Json"->>'DisplayOrder')::INT) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index for filtering by make
CREATE INDEX IX_VehicleModel_Make ON "VehicleModel"("Make", "IsActive");

-- Index for filtering by vehicle type
CREATE INDEX IX_VehicleModel_VehicleType ON "VehicleModel"("VehicleType", "IsActive");

-- Unique constraint on make + model combination
CREATE UNIQUE INDEX IX_VehicleModel_Make_Model ON "VehicleModel"("Make", "Model");

-- Index for display ordering
CREATE INDEX IX_VehicleModel_DisplayOrder ON "VehicleModel"("DisplayOrder", "IsActive");
