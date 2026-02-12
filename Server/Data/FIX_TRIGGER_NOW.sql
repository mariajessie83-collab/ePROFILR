-- EASIEST FIX: Just run these queries one by one in MySQL

-- 1. First, find the trigger name:
SHOW TRIGGERS WHERE `Table` = 'incidentreports';

-- 2. Look for any trigger that has "ResolutionNotes" in its definition
-- 3. Then drop it (replace 'TRIGGER_NAME' with actual name):
-- DROP TRIGGER IF EXISTS TRIGGER_NAME;

-- OR if you want to be safe, try these common trigger names:
DROP TRIGGER IF EXISTS incidentreports_audit;
DROP TRIGGER IF EXISTS incidentreports_before_update;
DROP TRIGGER IF EXISTS incidentreports_after_update;
DROP TRIGGER IF EXISTS incidentreports_update_log;
DROP TRIGGER IF EXISTS incidentreports_status_update;

-- Verify it's gone:
SHOW TRIGGERS WHERE `Table` = 'incidentreports';

