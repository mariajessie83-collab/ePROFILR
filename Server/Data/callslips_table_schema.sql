-- Table for storing generated call slips
-- MIGRATION: 
-- ALTER TABLE callslips MODIFY COLUMN IncidentID INT NULL;
-- ALTER TABLE callslips ADD COLUMN EscalationID INT NULL AFTER IncidentID;
-- ALTER TABLE callslips DROP FOREIGN KEY IF EXISTS callslips_ibfk_1;
-- ALTER TABLE callslips ADD CONSTRAINT fk_callslips_incident FOREIGN KEY (IncidentID) REFERENCES incidentreports(IncidentID) ON DELETE SET NULL;

CREATE TABLE IF NOT EXISTS callslips (
    CallSlipID INT AUTO_INCREMENT PRIMARY KEY,
    IncidentID INT NULL,
    EscalationID INT NULL,
    ComplainantName VARCHAR(500) NOT NULL,
    VictimName VARCHAR(500) NULL,
    RespondentName VARCHAR(500) NOT NULL,
    DateReported DATETIME NOT NULL,
    MeetingDate DATE NULL,
    MeetingTime TIME NULL,
    SchoolName VARCHAR(200) NOT NULL,
    PODTeacherName VARCHAR(100) NOT NULL,
    PODPosition VARCHAR(50) NOT NULL DEFAULT 'POD',
    GeneratedBy VARCHAR(100) NOT NULL,
    GeneratedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    INDEX idx_incidentid (IncidentID),
    INDEX idx_escalationid (EscalationID),
    INDEX idx_generateddate (GeneratedDate),
    INDEX idx_generatedby (GeneratedBy)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

