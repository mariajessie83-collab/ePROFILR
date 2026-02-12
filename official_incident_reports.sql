-- Official Incident Reports Table
-- This table stores incident reports filed by school officials (Teachers and POD)

CREATE TABLE IF NOT EXISTS OfficialIncidentReports (
    IncidentID INT AUTO_INCREMENT PRIMARY KEY,
    ReporterID INT NOT NULL, -- ID of the Teacher or POD filing the report
    ReporterRole VARCHAR(50) NOT NULL, -- 'Teacher' or 'POD'
    RespondentName VARCHAR(500) NOT NULL,
    IncidentType VARCHAR(100) NOT NULL,
    Description TEXT NOT NULL,
    DateReported DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    SchoolName VARCHAR(255) NOT NULL,
    Division VARCHAR(255) NOT NULL,
    Status VARCHAR(50) NOT NULL DEFAULT 'Reported',
    IsActive TINYINT(1) DEFAULT 1,
    INDEX idx_reporter (ReporterID),
    INDEX idx_school (SchoolName),
    INDEX idx_status (Status),
    INDEX idx_date (DateReported)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
