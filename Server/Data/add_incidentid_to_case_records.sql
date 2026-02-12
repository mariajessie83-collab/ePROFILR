-- Add IncidentID column to StudentProfileCaseRecords table
-- This links each case record to a specific incident report

-- Check if column exists before adding
SET @dbname = DATABASE();
SET @tablename = "StudentProfileCaseRecords";
SET @columnname = "IncidentID";
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (table_name = @tablename)
      AND (table_schema = @dbname)
      AND (column_name = @columnname)
  ) > 0,
  "SELECT 1",
  CONCAT("ALTER TABLE ", @tablename, " ADD COLUMN ", @columnname, " INT NULL AFTER RecordID")
));

PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Add foreign key constraint to link to IncidentReports table
SET @fkname = "fk_case_record_incident";
SET @preparedStatement2 = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE
      (table_name = @tablename)
      AND (table_schema = @dbname)
      AND (constraint_name = @fkname)
  ) > 0,
  "SELECT 1",
  CONCAT("ALTER TABLE ", @tablename, " ADD CONSTRAINT ", @fkname, " FOREIGN KEY (IncidentID) REFERENCES incidentreports(IncidentID) ON DELETE SET NULL")
));

PREPARE addFKIfNotExists FROM @preparedStatement2;
EXECUTE addFKIfNotExists;
DEALLOCATE PREPARE addFKIfNotExists;

-- Add index for better query performance
SET @indexname = "idx_incidentid";
SET @preparedStatement3 = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE
      (table_name = @tablename)
      AND (table_schema = @dbname)
      AND (index_name = @indexname)
  ) > 0,
  "SELECT 1",
  CONCAT("ALTER TABLE ", @tablename, " ADD INDEX ", @indexname, " (IncidentID)")
));

PREPARE addIndexIfNotExists FROM @preparedStatement3;
EXECUTE addIndexIfNotExists;
DEALLOCATE PREPARE addIndexIfNotExists;

SELECT 'IncidentID column added successfully to StudentProfileCaseRecords table' AS Result;
