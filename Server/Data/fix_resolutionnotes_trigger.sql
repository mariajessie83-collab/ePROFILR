-- Fix for ResolutionNotes trigger error
-- This script checks for and fixes triggers that reference the non-existent ResolutionNotes column

-- Step 1: Check existing triggers on incidentreports table
SHOW TRIGGERS WHERE `Table` = 'incidentreports';

-- Step 2: Drop the problematic trigger(s) that reference ResolutionNotes
-- NOTE: Replace 'trigger_name' with the actual trigger name found in Step 1
-- Common trigger names might be: incidentreports_audit, incidentreports_update_log, etc.

-- Example: If trigger is named 'incidentreports_audit'
-- DROP TRIGGER IF EXISTS incidentreports_audit;

-- Step 3: Recreate the trigger WITHOUT ResolutionNotes reference (if needed)
-- OR add ResolutionNotes column if it's actually needed:

-- Option A: Add ResolutionNotes column (if you need it)
-- ALTER TABLE incidentreports 
-- ADD COLUMN ResolutionNotes TEXT NULL AFTER Status;

-- Option B: If you don't need ResolutionNotes, find and drop the trigger
-- Run this query to find triggers referencing ResolutionNotes:
-- SELECT TRIGGER_NAME, EVENT_MANIPULATION, EVENT_OBJECT_TABLE, ACTION_STATEMENT
-- FROM INFORMATION_SCHEMA.TRIGGERS
-- WHERE EVENT_OBJECT_TABLE = 'incidentreports'
-- AND ACTION_STATEMENT LIKE '%ResolutionNotes%';

-- After finding the trigger, drop it:
-- DROP TRIGGER IF EXISTS [trigger_name];

-- Option C: If you need to keep the trigger but fix it, recreate without ResolutionNotes:
-- DROP TRIGGER IF EXISTS [trigger_name];
-- 
-- CREATE TRIGGER [trigger_name]
-- AFTER UPDATE ON incidentreports
-- FOR EACH ROW
-- BEGIN
--     -- Your trigger logic here WITHOUT ResolutionNotes reference
--     -- Example audit log without ResolutionNotes:
--     INSERT INTO audit_logs (table_name, record_id, old_value, new_value, updated_at)
--     VALUES ('incidentreports', NEW.IncidentID, OLD.Status, NEW.Status, NOW());
-- END;

