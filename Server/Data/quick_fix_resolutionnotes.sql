-- QUICK FIX: Drop trigger with ResolutionNotes
-- Run this in MySQL Workbench or phpMyAdmin

-- Step 1: Find the trigger name
SELECT TRIGGER_NAME 
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE EVENT_OBJECT_TABLE = 'incidentreports'
AND ACTION_STATEMENT LIKE '%ResolutionNotes%';

-- Step 2: Replace 'trigger_name_here' with the actual trigger name from Step 1 and run:
-- DROP TRIGGER IF EXISTS trigger_name_here;

-- OR use this automated version (run all at once):
SET @sql = NULL;

SELECT GROUP_CONCAT(
    CONCAT('DROP TRIGGER IF EXISTS `', TRIGGER_NAME, '`') 
    SEPARATOR '; '
)
INTO @sql
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE EVENT_OBJECT_TABLE = 'incidentreports'
AND ACTION_STATEMENT LIKE '%ResolutionNotes%'
AND TRIGGER_NAME IS NOT NULL;

SELECT IFNULL(@sql, 'SELECT "No triggers found with ResolutionNotes"') AS 'SQL to execute';

-- Uncomment the lines below to execute automatically:
-- SET @execute = CONCAT(@sql, ';');
-- PREPARE stmt FROM @execute;
-- EXECUTE stmt;
-- DEALLOCATE PREPARE stmt;

