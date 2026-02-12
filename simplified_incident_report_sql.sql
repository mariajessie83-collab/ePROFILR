-- Simplified Incident Reports Table
-- This table replaces the complex student-based incident report structure

-- Drop existing table if it exists
-- Drop existing table if it exists
-- DROP TABLE IF EXISTS SimplifiedIncidentReports;

-- Increase max_allowed_packet to 64MB to handle large base64 images
-- This fixes the 'Packet for query is too large' error
SET GLOBAL max_allowed_packet=67108864;

-- Create new table with Division column
CREATE TABLE IF NOT EXISTS SimplifiedIncidentReports (
    IncidentID INT AUTO_INCREMENT PRIMARY KEY,
    ViolationID INT NULL,
    FullName VARCHAR(200) NOT NULL,
    RespondentName VARCHAR(500) NOT NULL,
    AdviserName VARCHAR(500) NULL,
    VictimName VARCHAR(500) NULL,
    IncidentType VARCHAR(100) NOT NULL,
    Description TEXT NOT NULL,
    EvidencePhoto TEXT NULL,
    EvidencePhotoBase64 LONGTEXT NULL,
    ReferenceNumber VARCHAR(50) NULL,
    Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    SchoolName VARCHAR(255) NOT NULL,
    Division VARCHAR(255) NOT NULL,
    ReporterID INT NULL,
    ReporterRole VARCHAR(50) NULL,
    DateReported DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    IsActive TINYINT(1) DEFAULT 1,
    INDEX idx_reference (ReferenceNumber),
    INDEX idx_school (SchoolName),
    INDEX idx_division (Division),
    INDEX idx_status (Status),
    INDEX idx_date (DateReported),
    INDEX idx_reporter (ReporterID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- Add columns if they don't exist (for existing tables)
-- You can run these lines if you already have the table created without these columns
SET @dbname = DATABASE();
SET @tablename = "SimplifiedIncidentReports";

-- Add AdviserName
SET @columnname = "AdviserName";
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (table_name = @tablename)
      AND (table_schema = @dbname)
      AND (column_name = @columnname)
  ) > 0,
  "SELECT 1",
  "ALTER TABLE SimplifiedIncidentReports ADD COLUMN AdviserName VARCHAR(500) NULL AFTER RespondentName;"
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Add ReporterID
SET @columnname = "ReporterID";
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (table_name = @tablename)
      AND (table_schema = @dbname)
      AND (column_name = @columnname)
  ) > 0,
  "SELECT 1",
  "ALTER TABLE SimplifiedIncidentReports ADD COLUMN ReporterID INT NULL AFTER Division;"
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Add ReporterRole
SET @columnname = "ReporterRole";
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (table_name = @tablename)
      AND (table_schema = @dbname)
      AND (column_name = @columnname)
  ) > 0,
  "SELECT 1",
  "ALTER TABLE SimplifiedIncidentReports ADD COLUMN ReporterRole VARCHAR(50) NULL AFTER ReporterID;"
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;


-- Add HasVictim ENUM column to ViolationTypes table
ALTER TABLE ViolationTypes 
ADD COLUMN HasVictim ENUM('Yes', 'No') NOT NULL DEFAULT 'Yes' 
AFTER ViolationCategory;

-- Update ViolationTypes: Set violations WITHOUT victims to 'No'
-- These are typically dress code, policy violations, or self-related issues with no direct victim

UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 13;
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 14; -- Class Absences (Accumulated)
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 15; -- No Monday Flag Ceremony
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 16; -- Cutting/Escaping Classes
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 17; -- Loitering During Class
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 18; -- Unnecessary Noise
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 19; -- Littering
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 20; -- No/Shared ID Card
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 21; -- Improper Haircut (Boys)
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 23; -- Earrings (Boys)
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 26; -- Picking Fruits/Flowers
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 27; -- Inappropriate Accessories
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 28; -- Charging Gadgets in Class
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 29; -- Public Urination
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 30; -- Glaring Hair/Nail Colors
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 31; -- Makeup/High Heels (JHS)
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 32; -- Indecent Attire in Campus

-- Major violations without victims (policy/administrative violations)
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 37; -- Forgery/Tampering Records
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 43; -- Smoking/Vape in Campus
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 46; -- Entering Under Influence
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 47; -- Liquor in Campus
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 51; -- Unauthorized Entry/Exit
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 52; -- Unauthorized Use of Facilities
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 55; -- Unauthorized Fundraising
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 56; -- Willful Non‑Compliance
UPDATE ViolationTypes SET HasVictim = 'No' WHERE ViolationID = 57; -- Habitual Minor Violations


-- Note: Violations with victims (bullying, theft, assault, etc.) remain as 'Yes'

-- Update callslips table schema
ALTER TABLE callslips
MODIFY COLUMN ComplainantName VARCHAR(500) NOT NULL,
MODIFY COLUMN RespondentName VARCHAR(500) NOT NULL,
MODIFY COLUMN VictimName VARCHAR(500) NULL;

-- Remove legacy foreign key constraints from callslips table
-- These constraints pointed to the old incidentreports table
ALTER TABLE callslips DROP FOREIGN KEY IF EXISTS callslips_ibfk_1;
ALTER TABLE callslips DROP FOREIGN KEY IF EXISTS fk_callslips_incident;
