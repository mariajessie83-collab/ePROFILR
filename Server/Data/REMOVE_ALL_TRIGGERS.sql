-- REMOVE ALL TRIGGERS FROM incidentreports TABLE
-- Run this in MySQL to completely remove all triggers

-- Step 1: Show all triggers first (to see what will be deleted)
SHOW TRIGGERS WHERE `Table` = 'incidentreports';

-- Step 2: Drop ALL triggers on incidentreports table
-- This will get all trigger names and drop them automatically

SET @sql = NULL;

SELECT GROUP_CONCAT(
    CONCAT('DROP TRIGGER IF EXISTS `', TRIGGER_NAME, '`') 
    SEPARATOR '; '
)
INTO @sql
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE EVENT_OBJECT_TABLE = 'incidentreports';

-- Show what will be executed
SELECT IFNULL(CONCAT(@sql, ';'), 'No triggers found') AS 'Drop Commands';

-- Execute the drop commands
SET @dropSql = @sql;
SET @dropSql = CONCAT(@dropSql, ';');

PREPARE stmt FROM @dropSql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verify all triggers are gone
SHOW TRIGGERS WHERE `Table` = 'incidentreports';

