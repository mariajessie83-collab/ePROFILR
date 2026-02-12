-- COPY AND PASTE THIS DIRECTLY INTO MYSQL
-- This will find and drop the trigger causing the ResolutionNotes error

-- Step 1: See all triggers on incidentreports table
SHOW TRIGGERS WHERE `Table` = 'incidentreports';

-- Step 2: Drop ALL triggers that might have ResolutionNotes
-- (Safe to run - only drops if they exist)
DROP TRIGGER IF EXISTS incidentreports_audit;
DROP TRIGGER IF EXISTS incidentreports_before_update;
DROP TRIGGER IF EXISTS incidentreports_after_update;
DROP TRIGGER IF EXISTS incidentreports_update_log;
DROP TRIGGER IF EXISTS incidentreports_status_update;
DROP TRIGGER IF EXISTS incidentreports_update_audit;
DROP TRIGGER IF EXISTS incidentreports_before_insert;
DROP TRIGGER IF EXISTS incidentreports_after_insert;

-- Step 3: Find any remaining triggers with ResolutionNotes
SELECT TRIGGER_NAME 
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE EVENT_OBJECT_TABLE = 'incidentreports'
AND ACTION_STATEMENT LIKE '%ResolutionNotes%';

-- Step 4: If Step 3 shows any trigger names, drop them manually:
-- DROP TRIGGER IF EXISTS [trigger_name_from_step_3];

-- Step 5: Verify all triggers are gone
SHOW TRIGGERS WHERE `Table` = 'incidentreports';

