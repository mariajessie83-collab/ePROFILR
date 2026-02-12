-- Create CaseEscalations table for manual escalation feature
-- This table tracks when advisers manually send students with 3+ minor cases to POD

CREATE TABLE IF NOT EXISTS CaseEscalations (
    EscalationID INT PRIMARY KEY AUTO_INCREMENT,
    StudentName VARCHAR(255) NOT NULL,
    GradeLevel VARCHAR(50),
    Section VARCHAR(50),
    SchoolID INT,
    MinorCaseCount INT NOT NULL,
    CaseDetails TEXT COMMENT 'Violations triggering escalation (e.g. Minor violation names)',
    EscalatedBy VARCHAR(255) NOT NULL COMMENT 'Teacher/Adviser who escalated',
    TeacherID INT COMMENT 'ID of teacher who escalated',
    EscalatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Status VARCHAR(50) DEFAULT 'Active' COMMENT 'Active, Resolved, Withdrawn',
    Notes TEXT,
    IsActive TINYINT DEFAULT 1,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_student (StudentName, GradeLevel, Section),
    INDEX idx_status (Status, IsActive),
    INDEX idx_school (SchoolID),
    INDEX idx_teacher (TeacherID),
    INDEX idx_escalated_date (EscalatedDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Add comment to table
ALTER TABLE CaseEscalations COMMENT = 'Tracks manual escalation of students with 3+ minor cases to POD';

-- For existing DBs: add CaseDetails column if missing (run if table already existed without it)
-- ALTER TABLE CaseEscalations ADD COLUMN IF NOT EXISTS CaseDetails TEXT NULL COMMENT 'Violations triggering escalation' AFTER MinorCaseCount;
-- MySQL 8.0.12+: use below; older MySQL run: ALTER TABLE CaseEscalations ADD COLUMN CaseDetails TEXT NULL AFTER MinorCaseCount;
