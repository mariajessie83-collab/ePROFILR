-- Admin Accounts Table Schema
-- This table stores admin, school head, and division accounts

CREATE TABLE IF NOT EXISTS AdminAccounts (
    AdminAccountID INT AUTO_INCREMENT PRIMARY KEY,
    UserID INT NOT NULL,
    AccountType ENUM('admin', 'schoolhead', 'division') NOT NULL,
    FullName VARCHAR(200) NOT NULL,
    Email VARCHAR(100),
    PhoneNumber VARCHAR(20),
    
    -- School Head specific fields
    SchoolID INT NULL,
    SchoolName VARCHAR(200) NULL,
    School_ID VARCHAR(50) NULL,
    Division VARCHAR(100) NULL,
    Region VARCHAR(100) NULL,
    District VARCHAR(100) NULL,
    
    -- Division specific fields (only division name)
    DivisionName VARCHAR(100) NULL,
    
    -- Common fields
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedBy INT NULL, -- AdminAccountID of the creator
    DateCreated DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastModified DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (CreatedBy) REFERENCES AdminAccounts(AdminAccountID) ON DELETE SET NULL,
    
    INDEX idx_account_type (AccountType),
    INDEX idx_school_id (SchoolID),
    INDEX idx_division (Division),
    INDEX idx_region (Region),
    INDEX idx_is_active (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Add comments
ALTER TABLE AdminAccounts 
    COMMENT = 'Stores admin, school head, and division account information';

