-- Fix PricingRule table: Add StartDate/EndDate as computed columns
-- This fixes error: "column StartDate is of type date but expression is of type text"

-- Create immutable function for text to date conversion
CREATE OR REPLACE FUNCTION immutable_text_to_date(text) RETURNS DATE AS $$
    SELECT $1::DATE;
$$ LANGUAGE SQL IMMUTABLE STRICT;

-- Drop existing columns if they exist
ALTER TABLE "PricingRule" DROP COLUMN IF EXISTS "StartDate";
ALTER TABLE "PricingRule" DROP COLUMN IF EXISTS "EndDate";

-- Add computed columns using the immutable function
ALTER TABLE "PricingRule"
ADD COLUMN "StartDate" DATE GENERATED ALWAYS AS (immutable_text_to_date("Json"->>'StartDate')) STORED;

ALTER TABLE "PricingRule"
ADD COLUMN "EndDate" DATE GENERATED ALWAYS AS (immutable_text_to_date("Json"->>'EndDate')) STORED;

-- Create index for date range queries
DROP INDEX IF EXISTS IX_PricingRule_DateRange;
CREATE INDEX IX_PricingRule_DateRange ON "PricingRule"("StartDate", "EndDate", "IsActive");
