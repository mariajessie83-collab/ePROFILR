-- Add AdviserName column to SimplifiedIncidentReports
ALTER TABLE SimplifiedIncidentReports 
ADD COLUMN AdviserName VARCHAR(500) DEFAULT '' AFTER RespondentName;
