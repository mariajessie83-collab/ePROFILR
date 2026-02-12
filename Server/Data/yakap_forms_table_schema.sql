-- Create Y.A.K.A.P. Reflection Forms Table
-- YUNIT NA AAKAY SA KABATAAN PARA SA ASAL AT PAGPAPAKATAO

CREATE TABLE IF NOT EXISTS YakapForms (
    YakapFormID INT AUTO_INCREMENT PRIMARY KEY,
    RecordID INT NOT NULL,
    StudentName VARCHAR(200) NOT NULL,
    GradeAndSection VARCHAR(100) NOT NULL,
    DateOfSession DATE NOT NULL,
    FacilitatorCounselor VARCHAR(200) NOT NULL,
    SchoolName VARCHAR(200) NOT NULL,
    SchoolID INT NULL,
    
    -- Bahagi I – Pag-unawa sa Aking Karanasan
    Question1_AnoAngNangyari TEXT NOT NULL,
    Question2_AnoAngIniisipOFeelings TEXT NOT NULL,
    
    -- Bahagi II – Pananagutan sa Ginawang Pagkilos
    Question3_AnoAngEpektoSaIba TEXT NOT NULL,
    Question4_AnoAngIniisipTungkolSaDesisyon TEXT NOT NULL,
    
    -- Bahagi III – Pagharap sa Hinaharap Gamit ang Positibong Disiplina
    Question5_AnoAngGagawinMongIba TEXT NOT NULL,
    Question6_AnongPositibongPagpapahalaga TEXT NOT NULL,
    Question7_MensaheParaSaHinaharap TEXT NOT NULL,
    
    -- Status: Sent, InProgress, Completed
    Status VARCHAR(50) NOT NULL DEFAULT 'Sent',
    
    -- Metadata
    DateCreated DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DateModified DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(100) NOT NULL,
    ModifiedBy VARCHAR(100) NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    
    -- Foreign key to link to case records
    FOREIGN KEY (RecordID) REFERENCES StudentProfileCaseRecords(RecordID) ON DELETE CASCADE,
    
    -- Foreign key to link to schools (optional)
    FOREIGN KEY (SchoolID) REFERENCES schools(SchoolID) ON DELETE SET NULL,
    
    -- Indexes for better query performance
    INDEX idx_recordid (RecordID),
    INDEX idx_studentname (StudentName),
    INDEX idx_dateofsession (DateOfSession),
    INDEX idx_schoolid (SchoolID),
    INDEX idx_schoolname (SchoolName),
    INDEX idx_createdby (CreatedBy),
    INDEX idx_isactive (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Add comment to table
ALTER TABLE YakapForms COMMENT = 'Y.A.K.A.P. Reflection Forms - YUNIT NA AAKAY SA KABATAAN PARA SA ASAL AT PAGPAPAKATAO';

SELECT 'Y.A.K.A.P. Forms table created successfully' AS Result;

