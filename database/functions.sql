-- Helper functions for PostgreSQL generated columns
-- DATE generated columns from JSON text require IMMUTABLE functions

CREATE OR REPLACE FUNCTION immutable_text_to_date(text) RETURNS DATE AS $$
BEGIN
    RETURN $1::DATE;
EXCEPTION WHEN OTHERS THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Helper function for TIMESTAMPTZ generated columns from JSON text
CREATE OR REPLACE FUNCTION immutable_text_to_timestamptz(text) RETURNS TIMESTAMPTZ AS $$
BEGIN
    RETURN $1::TIMESTAMPTZ;
EXCEPTION WHEN OTHERS THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql IMMUTABLE;
