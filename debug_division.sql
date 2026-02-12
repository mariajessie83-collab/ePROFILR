-- 1. Check if there are ANY records in SimplifiedIncidentReports
SELECT 'Total Records' as CheckName, COUNT(*) as Count FROM SimplifiedIncidentReports;

-- 2. Check the distinct Division names in the reports table
SELECT 'Reports by Division' as CheckName, Division, COUNT(*) as Count 
FROM SimplifiedIncidentReports 
GROUP BY Division;

-- 3. Check School Names and their Divisions from the schools table
SELECT 'Schools Table Smaple' as CheckName, SchoolName, Division 
FROM schools 
LIMIT 10;

-- 4. Check for potential mismatches that cause the LEFT JOIN to fail finding a Division
SELECT 
    sir.IncidentID,
    sir.SchoolName as ReportSchool,
    s.SchoolName as DbSchool,
    sir.Division as ReportDivision,
    s.Division as DbDivision
FROM SimplifiedIncidentReports sir
LEFT JOIN schools s ON sir.SchoolName = s.SchoolName
LIMIT 20;

-- 5. Check explicitly for South Cotabato (adjust string as needed)
SELECT * 
FROM SimplifiedIncidentReports sir
LEFT JOIN schools s ON sir.SchoolName = s.SchoolName
WHERE sir.Division LIKE '%Cotabato%' OR s.Division LIKE '%Cotabato%';
