-- Add Status column to existing YakapForms table
-- Status values: Sent, InProgress, Completed

-- Check if column exists before adding
SET @dbname = DATABASE();
SET @tablename = "YakapForms";
SET @columnname = "Status";
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (table_name = @tablename)
      AND (table_schema = @dbname)
      AND (column_name = @columnname)
  ) > 0,
  "SELECT 'Status column already exists in YakapForms table' AS Result",
  CONCAT("ALTER TABLE ", @tablename, " ADD COLUMN ", @columnname, " VARCHAR(50) NOT NULL DEFAULT 'Sent' AFTER Question7_MensaheParaSaHinaharap")
));

PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Update existing records to have 'Sent' status if they don't have answers yet
UPDATE YakapForms 
SET Status = CASE 
    WHEN (Question1_AnoAngNangyari IS NULL OR Question1_AnoAngNangyari = '') 
         AND (Question2_AnoAngIniisipOFeelings IS NULL OR Question2_AnoAngIniisipOFeelings = '') 
         AND (Question3_AnoAngEpektoSaIba IS NULL OR Question3_AnoAngEpektoSaIba = '') THEN 'Sent'
    WHEN (Question1_AnoAngNangyari IS NOT NULL AND Question1_AnoAngNangyari != '') 
         AND (Question2_AnoAngIniisipOFeelings IS NOT NULL AND Question2_AnoAngIniisipOFeelings != '') 
         AND (Question3_AnoAngEpektoSaIba IS NOT NULL AND Question3_AnoAngEpektoSaIba != '')
         AND (Question4_AnoAngIniisipTungkolSaDesisyon IS NOT NULL AND Question4_AnoAngIniisipTungkolSaDesisyon != '')
         AND (Question5_AnoAngGagawinMongIba IS NOT NULL AND Question5_AnoAngGagawinMongIba != '')
         AND (Question6_AnongPositibongPagpapahalaga IS NOT NULL AND Question6_AnongPositibongPagpapahalaga != '')
         AND (Question7_MensaheParaSaHinaharap IS NOT NULL AND Question7_MensaheParaSaHinaharap != '') THEN 'Completed'
    ELSE 'InProgress'
END
WHERE Status = 'Sent';

SELECT 'Status column added successfully to YakapForms table' AS Result;

