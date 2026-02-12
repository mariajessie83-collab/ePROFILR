-- Remove ResolutionNotes from database triggers
-- This script finds and drops triggers that reference the non-existent ResolutionNotes column

-- Step 1: Find all triggers on incidentreports table that reference ResolutionNotes
SELECT 
    TRIGGER_NAME,
    EVENT_MANIPULATION,
    EVENT_OBJECT_TABLE,
    ACTION_STATEMENT
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE EVENT_OBJECT_TABLE = 'incidentreports'
AND ACTION_STATEMENT LIKE '%ResolutionNotes%';

-- Step 2: Drop all triggers that reference ResolutionNotes
-- Run this query first to see the trigger names, then uncomment and execute the DROP statements below

-- Drop trigger(s) - Replace 'trigger_name' with actual trigger name from Step 1
-- Common trigger names might be:
-- DROP TRIGGER IF EXISTS incidentreports_audit;
-- DROP TRIGGER IF EXISTS incidentreports_update_log;
-- DROP TRIGGER IF EXISTS incidentreports_before_update;
-- DROP TRIGGER IF EXISTS incidentreports_after_update;

-- Step 3: If you need to recreate the trigger without ResolutionNotes, use this template:
-- CREATE TRIGGER incidentreports_after_update
-- AFTER UPDATE ON incidentreports
-- FOR EACH ROW
-- BEGIN
--     -- Your trigger logic here WITHOUT ResolutionNotes
--     -- Example: Log status changes
--     INSERT INTO audit_logs (table_name, record_id, old_status, new_status, updated_at)
--     VALUES ('incidentreports', NEW.IncidentID, OLD.Status, NEW.Status, NOW());
-- END;

-- Alternative: If you want to automatically drop ALL triggers that reference ResolutionNotes
-- (Run this after checking Step 1 results)
SET @sql = NULL;
SELECT GROUP_CONCAT(CONCAT('DROP TRIGGER IF EXISTS ', TRIGGER_NAME, ';') SEPARATOR ' ')
INTO @sql
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE EVENT_OBJECT_TABLE = 'incidentreports'
AND ACTION_STATEMENT LIKE '%ResolutionNotes%';

SELECT @sql AS 'SQL to execute';
-- Copy the output and execute it, or uncomment the line below to execute automatically:
-- PREPARE stmt FROM @sql;
-- EXECUTE stmt;
-- DEALLOCATE PREPARE stmt;

