-- Add parent/guardian contact columns for simplified student profile case records
-- Note: Some MySQL versions don't support 'ADD COLUMN IF NOT EXISTS' for multiple columns,
-- so we keep it simple with plain ADD COLUMN statements. Run this script only once.

ALTER TABLE SimplifiedStudentProfileCaseRecords
    ADD COLUMN ParentContactType VARCHAR(20) NULL;

ALTER TABLE SimplifiedStudentProfileCaseRecords
    ADD COLUMN ParentContactName VARCHAR(200) NULL;

ALTER TABLE SimplifiedStudentProfileCaseRecords
    ADD COLUMN ParentMeetingDate DATETIME NULL;

