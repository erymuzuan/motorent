-- Migration: Add Email computed column and indexes for Renter table
-- Purpose: Fix slow lookup in RentalHistory page (Tourist portal)
-- Date: 2026-02-20

-- Add Email computed column (extracted from JSONB for indexing)
ALTER TABLE "Renter" ADD COLUMN IF NOT EXISTS
    "Email" VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Email')::VARCHAR(200)) STORED;

-- Add indexes for fast lookup by email/phone
CREATE INDEX IF NOT EXISTS IX_Renter_Email ON "Renter"("Email");
CREATE INDEX IF NOT EXISTS IX_Renter_Phone ON "Renter"("Phone");
