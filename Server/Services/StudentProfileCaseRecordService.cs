using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using SharedProject;
using Microsoft.Extensions.Logging;
using Server.Data;

namespace Server.Services
{
    public class StudentProfileCaseRecordService
    {
        private readonly Dbconnections _dbConnections;
        private readonly ILogger<StudentProfileCaseRecordService> _logger;

        public StudentProfileCaseRecordService(Dbconnections dbConnections, ILogger<StudentProfileCaseRecordService> logger)
        {
            _dbConnections = dbConnections;
            _logger = logger;
        }

        public async Task<List<TeacherSearchResult>> SearchTeachersAsync(string searchTerm, string? schoolName = null)
        {
            var teachers = new List<TeacherSearchResult>();

            try
            {
                _logger.LogInformation("Searching teachers with term: {SearchTerm}, school: {SchoolName}", searchTerm, schoolName);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT t.TeacherID, t.TeacherName, t.Position, t.GradeLevel, t.Section, t.Strand,
                           COALESCE(s.SchoolName, t.SchoolName) as SchoolName
                    FROM teachers t 
                    INNER JOIN users u ON t.UserID = u.UserID
                    LEFT JOIN schools s ON t.SchoolID = s.SchoolID
                    WHERE t.IsActive = 1 AND u.IsActive = 1 
                    AND (t.TeacherName LIKE @SearchTerm OR t.Position LIKE @SearchTerm)";

                // Add school filter if provided - MUST match school (case-insensitive and trimmed)
                // Only show teachers where school name matches exactly (not NULL, not empty)
                if (!string.IsNullOrEmpty(schoolName))
                {
                    // Use TRIM and case-insensitive comparison to ensure proper matching
                    // Exclude teachers with NULL or empty school names
                    query += @" AND (
                        (s.SchoolName IS NOT NULL AND TRIM(UPPER(s.SchoolName)) = TRIM(UPPER(@SchoolName))) OR 
                        (t.SchoolName IS NOT NULL AND t.SchoolName != '' AND TRIM(UPPER(t.SchoolName)) = TRIM(UPPER(@SchoolName)))
                    )";
                    _logger.LogInformation("Applying strict school filter: {SchoolName}", schoolName);
                }

                query += " ORDER BY t.TeacherName LIMIT 10";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                
                if (!string.IsNullOrEmpty(schoolName))
                {
                    // Trim the school name to remove any whitespace issues
                    command.Parameters.AddWithValue("@SchoolName", schoolName.Trim());
                    _logger.LogInformation("School filter parameter set to: {SchoolName}", schoolName.Trim());
                }
                
                _logger.LogInformation("Executing query: {Query}", query);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var teacherSchoolName = reader.IsDBNull("SchoolName") ? "NULL" : reader.GetString("SchoolName");
                    var teacher = new TeacherSearchResult
                    {
                        TeacherID = reader.GetInt32("TeacherID"),
                        TeacherName = reader.IsDBNull("TeacherName") ? "Unknown" : reader.GetString("TeacherName"),
                        Position = reader.IsDBNull("Position") ? "" : reader.GetString("Position"),
                        GradeHandle = reader.IsDBNull("GradeLevel") ? "" : reader.GetString("GradeLevel"),
                        SectionHandle = reader.IsDBNull("Section") ? "" : reader.GetString("Section"),
                        TrackStrand = reader.IsDBNull("Strand") ? "" : reader.GetString("Strand"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName")
                    };
                    teachers.Add(teacher);
                    _logger.LogInformation("Found teacher: {TeacherName} (ID: {TeacherID}) from School: '{TeacherSchool}' | Section: '{Section}' | Strand: '{Strand}'", 
                        teacher.TeacherName, teacher.TeacherID, teacherSchoolName, teacher.SectionHandle, teacher.TrackStrand);
                }

                _logger.LogInformation("Found {Count} teachers matching search term (with school filter: '{SchoolName}')", 
                    teachers.Count, schoolName?.Trim() ?? "NONE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching teachers: {Message}", ex.Message);
            }

            return teachers;
        }

        public async Task<List<StudentAdviserInfo>> GetStudentAdviserAsync(string studentName, string? schoolName = null)
        {
            var matches = new List<StudentAdviserInfo>();

            try
            {
                _logger.LogInformation("=== GetStudentAdviserAsync START ===");
                _logger.LogInformation("Looking up adviser for student: '{StudentName}', School filter: '{SchoolName}'", studentName, schoolName);
                var connectionString = _dbConnections.GetConnection();

                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Database connection opened successfully");

                var normalizedStudentName = NormalizeStudentName(studentName);
                _logger.LogInformation("Normalized student name: '{NormalizedName}'", normalizedStudentName);

                // More aggressive matching - try to find student even with different name formats
                // Robust approach to column names: fetch common columns first
                var studentQuery = @"
                    SELECT *
                    FROM students s
                    LEFT JOIN schools sc ON s.SchoolID = sc.SchoolID
                    WHERE s.IsActive = 1
                      AND (
                          UPPER(TRIM(s.StudentName)) = UPPER(TRIM(@StudentName))
                          OR UPPER(REPLACE(REPLACE(REPLACE(s.StudentName, ',', ''), '.', ''), ' ', '')) = @NormalizedStudentName
                          OR UPPER(TRIM(s.StudentName)) LIKE CONCAT('%', UPPER(TRIM(@StudentName)), '%')
                          OR UPPER(TRIM(@StudentName)) LIKE CONCAT('%', UPPER(TRIM(s.StudentName)), '%')
                      )
                    ORDER BY 
                        CASE WHEN UPPER(TRIM(s.StudentName)) = UPPER(TRIM(@StudentName)) THEN 0 ELSE 1 END,
                        s.DateRegister DESC, 
                        s.StudentID DESC";

                await using var studentCommand = new MySqlCommand(studentQuery, connection);
                studentCommand.Parameters.AddWithValue("@StudentName", studentName);
                studentCommand.Parameters.AddWithValue("@NormalizedStudentName", normalizedStudentName);
                
                _logger.LogInformation("Executing Students table query (SELECT * for robustness)...");

                var studentRows = new List<(int StudentId, string? StudentName, int? TeacherId, string? GradeLevel, string? Section, string? TrackStrand, string? SchoolName, DateTime? DateRegister, string? FathersName, string? MothersName, string? GuardianName, string? GuardianContact, string? ContactPerson, string? Sex, DateTime? BirthDate, string? Address)>();

                await using var reader = await studentCommand.ExecuteReaderAsync();
                int rowCount = 0;
                while (await reader.ReadAsync())
                {
                    rowCount++;
                    var studentId = reader.GetInt32("StudentID");
                    var studentNameFromDb = reader.IsDBNull("StudentName") ? null : reader.GetString("StudentName");
                    var teacherId = reader.IsDBNull("TeacherID") ? (int?)null : reader.GetInt32("TeacherID");
                    var gradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel");
                    var section = reader.IsDBNull("Section") ? null : reader.GetString("Section");
                    
                    // Robustly try to find any column that might represent Track/Strand
                    string? trackStrand = null;
                    try {
                        // Priority: 1. Strand, 2. TrackStrand
                        int strandOrdinal = -1;
                        try { strandOrdinal = reader.GetOrdinal("Strand"); } catch { }
                        
                        int trackStrandOrdinal = -1;
                        try { trackStrandOrdinal = reader.GetOrdinal("TrackStrand"); } catch { }

                        if (strandOrdinal != -1 && !reader.IsDBNull(strandOrdinal))
                        {
                            trackStrand = reader.GetString(strandOrdinal);
                        }
                        else if (trackStrandOrdinal != -1 && !reader.IsDBNull(trackStrandOrdinal))
                        {
                            trackStrand = reader.GetString(trackStrandOrdinal);
                        }
                    } catch { }

                    var schoolNameFromDb = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName");
                    var dateRegister = reader.IsDBNull("DateRegister") ? (DateTime?)null : reader.GetDateTime("DateRegister");
                    var fathersName = reader.IsDBNull("FathersName") ? null : reader.GetString("FathersName");
                    var mothersName = reader.IsDBNull("MothersName") ? null : reader.GetString("MothersName");
                    var guardianName = reader.IsDBNull("GuardianName") ? null : reader.GetString("GuardianName");
                    var guardianContact = reader.IsDBNull("GuardianContact") ? null : reader.GetString("GuardianContact");
                    var contactPerson = reader.IsDBNull("ContactPerson") ? null : reader.GetString("ContactPerson");
                    
                    var sex = reader.IsDBNull("Gender") ? null : reader.GetString("Gender");
                    var birthDate = reader.IsDBNull("BirthDate") ? (DateTime?)null : reader.GetDateTime("BirthDate");
                    var address = reader.IsDBNull("Address") ? null : reader.GetString("Address");
                    
                    _logger.LogInformation("Found student row {RowNum}: StudentName={StudentName}, ID={StudentId}, Grade={Grade}, Section={Section}, Strand={Strand}, TeacherId={TeacherId}", 
                        rowCount, studentNameFromDb, studentId, gradeLevel, section, trackStrand, teacherId);

                    // Robust Strand Fallback: If strand is still null from Students table, try checking previous case records for this student name
                    if (string.IsNullOrWhiteSpace(trackStrand))
                    {
                        var caseFallback = await GetGradeSectionFromCaseRecordsAsync(connection, studentNameFromDb ?? studentName);
                        if (caseFallback.HasValue && !string.IsNullOrWhiteSpace(caseFallback.Value.TrackStrand))
                        {
                            trackStrand = caseFallback.Value.TrackStrand;
                            _logger.LogInformation("Strand fallback found in previous case records: {Strand}", trackStrand);
                        }
                    }
                    
                    studentRows.Add((studentId, studentNameFromDb, teacherId, gradeLevel, section, trackStrand, schoolNameFromDb, dateRegister, fathersName, mothersName, guardianName, guardianContact, contactPerson, sex, birthDate, address));
                }
                
                _logger.LogInformation("Total student rows found: {RowCount}", rowCount);

                await reader.DisposeAsync();

                foreach (var row in studentRows)
                {
                    var adviserName = await ResolveAdviserNameAsync(connection, row.TeacherId, row.GradeLevel, row.Section, schoolName ?? row.SchoolName);

                    matches.Add(new StudentAdviserInfo
                    {
                        StudentId = row.StudentId,
                        StudentName = row.StudentName, // Use the name from the database row
                        AdviserName = adviserName,
                        GradeLevel = row.GradeLevel,
                        Section = row.Section,
                        TrackStrand = row.TrackStrand,
                        SchoolName = row.SchoolName,
                        TeacherId = row.TeacherId,
                        Source = "Student Records",
                        Timestamp = row.DateRegister,
                        FathersName = row.FathersName,
                        MothersName = row.MothersName,
                        GuardianName = row.GuardianName,
                        GuardianContact = row.GuardianContact,
                        ContactPerson = row.ContactPerson,
                        Sex = row.Sex,
                        BirthDate = row.BirthDate,
                        Address = row.Address
                    });
                }

                _logger.LogInformation("Student records matches before incident fallback: {Count}", matches.Count);
                
                // Aggressive fallback for Track/Strand: If matches exist but TrackStrand is missing for Grade 11/12
                if (matches.Any())
                {
                    foreach (var m in matches)
                    {
                        if (string.IsNullOrWhiteSpace(m.TrackStrand) && (m.GradeLevel?.Contains("11") == true || m.GradeLevel?.Contains("12") == true))
                        {
                            _logger.LogInformation("Match found for {Name} but missing TrackStrand for Grade {Grade}. Checking historical fallback...", m.StudentName, m.GradeLevel);
                            var caseFallback = await GetGradeSectionFromCaseRecordsAsync(connection, m.StudentName ?? studentName);
                            if (caseFallback.HasValue && !string.IsNullOrWhiteSpace(caseFallback.Value.TrackStrand))
                            {
                                m.TrackStrand = caseFallback.Value.TrackStrand;
                                _logger.LogInformation("Historical TrackStrand found for {Name}: {Strand}", m.StudentName, m.TrackStrand);
                            }
                        }
                    }
                }

                if (!matches.Any())
                {
                    _logger.LogInformation("No matches from Students table, trying incident reports fallback...");
                    matches.AddRange(await GetIncidentReportStudentInfoAsync(studentName, null));
                    _logger.LogInformation("Incident reports fallback returned: {Count} matches", matches.Count);
                    
                    // If incident reports also don't have Grade/Section, try to get from existing case records
                    if (matches.Any() && matches.All(m => string.IsNullOrWhiteSpace(m.GradeLevel)))
                    {
                        _logger.LogInformation("No Grade/Section from incident reports, checking existing case records...");
                        var caseRecordInfo = await GetGradeSectionFromCaseRecordsAsync(connection, studentName);
                        if (caseRecordInfo.HasValue)
                        {
                            _logger.LogInformation("Found Grade/Section from case records: Grade={Grade}, Section={Section}", 
                                caseRecordInfo.Value.GradeLevel, caseRecordInfo.Value.Section);
                            
                            // Update all matches with this info
                            foreach (var match in matches)
                            {
                                if (string.IsNullOrWhiteSpace(match.GradeLevel))
                                {
                                    match.GradeLevel = caseRecordInfo.Value.GradeLevel;
                                    match.Section = caseRecordInfo.Value.Section;
                                    match.TrackStrand = caseRecordInfo.Value.TrackStrand;
                                }
                            }
                        }
                    }
                }

                matches = matches
                    .OrderByDescending(m => m.Timestamp ?? DateTime.MinValue)
                    .ThenByDescending(m => m.StudentId ?? 0)
                    .ToList();
                    
                _logger.LogInformation("=== GetStudentAdviserAsync END === Total matches: {Count}", matches.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving adviser for student {StudentName}: {Message}", studentName, ex.Message);
            }

            return matches;
        }

        public async Task<List<string>> GetViolationTypesAsync(string? category = null)
        {
            var violations = new List<string>();

            try
            {
                _logger.LogInformation("Getting violation types for category: {Category}", category);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ViolationName 
                    FROM violationtypes 
                    WHERE IsActive = 1";

                if (!string.IsNullOrEmpty(category))
                {
                    query += " AND ViolationCategory = @Category";
                }

                query += " ORDER BY ViolationName";

                using var command = new MySqlCommand(query, connection);
                if (!string.IsNullOrEmpty(category))
                {
                    command.Parameters.AddWithValue("@Category", category);
                }

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    violations.Add(reader.GetString("ViolationName"));
                }

                _logger.LogInformation("Retrieved {Count} violation types", violations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting violation types: {Message}", ex.Message);
                
                // Fallback to hardcoded violations if database fails
                violations = GetDefaultViolationTypes();
            }

            return violations;
        }

        public async Task<string?> GetViolationCategoryAsync(string violationName)
        {
            try
            {
                _logger.LogInformation("Getting violation category for violation: {ViolationName}", violationName);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Use case-insensitive comparison with LIKE to handle any whitespace or case differences
                // Also try exact match first, then case-insensitive
                var query = @"
                    SELECT ViolationCategory 
                    FROM violationtypes 
                    WHERE (ViolationName = @ViolationName 
                           OR LOWER(TRIM(ViolationName)) = LOWER(TRIM(@ViolationName)))
                    AND (IsActive = 1 OR IsActive IS NULL)
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                var trimmedName = violationName?.Trim() ?? "";
                command.Parameters.AddWithValue("@ViolationName", trimmedName);

                _logger.LogInformation("Executing query for violation: '{ViolationName}'", trimmedName);
                var result = await command.ExecuteScalarAsync();
                
                if (result != null && result != DBNull.Value)
                {
                    var category = result.ToString()?.Trim();
                    _logger.LogInformation("✅ Found category '{Category}' for violation '{ViolationName}'", category, violationName);
                    return category;
                }
                else
                {
                    _logger.LogWarning("❌ No category found for violation '{ViolationName}'", trimmedName);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting violation category for '{ViolationName}': {Message}", violationName, ex.Message);
                return null;
            }
        }

        private List<string> GetDefaultViolationTypes()
        {
            return new List<string>
            {
                "Late/Unauthorized Absence",
                "Dress Code Violation",
                "Disruptive Behavior",
                "Use of Profane Language",
                "Cheating/Plagiarism",
                "Bullying/Harassment",
                "Fighting/Physical Altercation",
                "Drug/Alcohol Possession",
                "Vandalism",
                "Theft",
                "Truancy",
                "Insubordination",
                "Other"
            };
        }

        public async Task<int> GetCaseRecordsTotalCountAsync(string? status = null)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var countQuery = @"
                    SELECT COUNT(*)
                    FROM studentprofilecaserecords
                    WHERE IsActive = 1";

                if (!string.IsNullOrEmpty(status))
                {
                    countQuery += " AND Status = @Status";
                }

                using var countCommand = new MySqlCommand(countQuery, connection);
                
                if (!string.IsNullOrEmpty(status))
                {
                    countCommand.Parameters.AddWithValue("@Status", status);
                }

                var result = await countCommand.ExecuteScalarAsync();
                var totalCount = result != null ? Convert.ToInt32(result) : 0;

                _logger.LogInformation("Total case records count: {TotalCount} (status filter: {Status})", totalCount, status ?? "all");
                return totalCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records total count: {Message}", ex.Message);
                return 0;
            }
        }

        public async Task<List<StudentProfileCaseRecordSummary>> GetCaseRecordsAsync(string? status = null, int page = 1, int pageSize = 10)
        {
            var records = new List<StudentProfileCaseRecordSummary>();

            try
            {
                _logger.LogInformation("Getting case records with status: {Status}", status);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // First check total count without filters to see if there are any records
                var countQuery = "SELECT COUNT(*) FROM studentprofilecaserecords";
                using var countCommand = new MySqlCommand(countQuery, connection);
                var totalCount = await countCommand.ExecuteScalarAsync();
                _logger.LogInformation("Total records in database (without filters): {TotalCount}", totalCount);

                var query = @"
                    SELECT RecordID, IncidentID, StudentOffenderName, GradeLevel, Section, ViolationCommitted, LevelOfOffense, DateOfOffense, Status
                    FROM studentprofilecaserecords
                    WHERE (IsActive = 1 OR IsActive IS NULL)";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND Status = @Status";
                }

                query += " ORDER BY DateOfOffense DESC LIMIT @Offset, @PageSize";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                _logger.LogInformation("Executing query: {Query}", query);
                _logger.LogInformation("Query parameters - Offset: {Offset}, PageSize: {PageSize}, Status: {Status}", 
                    (page - 1) * pageSize, pageSize, status ?? "all");

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var record = new StudentProfileCaseRecordSummary
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        StudentOffenderName = reader.GetString("StudentOffenderName"),
                        GradeLevel = reader.GetString("GradeLevel"),
                        Section = reader.GetString("Section"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        LevelOfOffense = reader.GetString("LevelOfOffense"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        Status = reader.GetString("Status")
                    };
                    records.Add(record);
                }

                _logger.LogInformation("Retrieved {Count} case records from database", records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records: {Message}\nStack trace: {StackTrace}", ex.Message, ex.StackTrace);
                throw; // Re-throw to see error in controller
            }

            return records;
        }

        public async Task<List<StudentProfileCaseRecordSummary>> GetCaseRecordsByTeacherAsync(int teacherId, string? status = null, int page = 1, int pageSize = 10)
        {
            var records = new List<StudentProfileCaseRecordSummary>();

            try
            {
                _logger.LogInformation("Getting all case records for teacher ID: {TeacherId}, status: {Status}", teacherId, status);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Query to get all case records for students belonging to the teacher
                // Match by StudentOffenderName, GradeLevel, and Section (all case-insensitive and trimmed)
                var query = @"
                    SELECT DISTINCT cr.RecordID, cr.IncidentID, cr.StudentOffenderName, cr.GradeLevel, cr.Section, 
                           cr.ViolationCommitted, cr.LevelOfOffense, cr.DateOfOffense, cr.Status
                    FROM studentprofilecaserecords cr
                    INNER JOIN students s ON 
                        UPPER(TRIM(cr.StudentOffenderName)) = UPPER(TRIM(s.StudentName))
                        AND UPPER(TRIM(cr.GradeLevel)) = UPPER(TRIM(s.GradeLevel))
                        AND UPPER(TRIM(cr.Section)) = UPPER(TRIM(s.Section))
                    WHERE (cr.IsActive = 1 OR cr.IsActive IS NULL)
                        AND s.TeacherID = @TeacherID
                        AND s.IsActive = 1";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND cr.Status = @Status";
                }

                query += " ORDER BY cr.DateOfOffense DESC LIMIT @Offset, @PageSize";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherID", teacherId);
                command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                _logger.LogInformation("Executing query: {Query}", query);
                _logger.LogInformation("Query parameters - TeacherID: {TeacherID}, Offset: {Offset}, PageSize: {PageSize}, Status: {Status}", 
                    teacherId, (page - 1) * pageSize, pageSize, status ?? "all");

                // Debug: Check if there are any students for this teacher
                var studentCountQuery = "SELECT COUNT(*) FROM students WHERE TeacherID = @TeacherID AND IsActive = 1";
                using var studentCountCommand = new MySqlCommand(studentCountQuery, connection);
                studentCountCommand.Parameters.AddWithValue("@TeacherID", teacherId);
                var studentCount = await studentCountCommand.ExecuteScalarAsync();
                _logger.LogInformation("Total students for teacher {TeacherID}: {StudentCount}", teacherId, studentCount);

                // Debug: Check if there are any case records
                var caseCountQuery = "SELECT COUNT(*) FROM studentprofilecaserecords WHERE (IsActive = 1 OR IsActive IS NULL)";
                using var caseCountCommand = new MySqlCommand(caseCountQuery, connection);
                var caseCount = await caseCountCommand.ExecuteScalarAsync();
                _logger.LogInformation("Total case records in database: {CaseCount}", caseCount);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var record = new StudentProfileCaseRecordSummary
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        StudentOffenderName = reader.GetString("StudentOffenderName"),
                        GradeLevel = reader.GetString("GradeLevel"),
                        Section = reader.GetString("Section"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        LevelOfOffense = reader.GetString("LevelOfOffense"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        Status = reader.GetString("Status")
                    };
                    records.Add(record);
                }

                _logger.LogInformation("Retrieved {Count} case records for teacher {TeacherID}", records.Count, teacherId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records for teacher {TeacherID}: {Message}\nStack trace: {StackTrace}", teacherId, ex.Message, ex.StackTrace);
                throw;
            }

            return records;
        }

        public async Task<List<StudentProfileCaseRecordSummary>> GetMinorCaseRecordsByTeacherAsync(int teacherId, string? status = null, int page = 1, int pageSize = 10)
        {
            var records = new List<StudentProfileCaseRecordSummary>();

            try
            {
                _logger.LogInformation("Getting minor case records for teacher ID: {TeacherId}, status: {Status}", teacherId, status);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Query to get minor case records for students belonging to the teacher
                // Match by StudentOffenderName, GradeLevel, and Section (all case-insensitive and trimmed)
                var query = @"
                    SELECT DISTINCT cr.RecordID, cr.IncidentID, cr.StudentOffenderName, cr.GradeLevel, cr.Section, 
                           cr.ViolationCommitted, cr.LevelOfOffense, cr.DateOfOffense, cr.Status
                    FROM studentprofilecaserecords cr
                    INNER JOIN students s ON 
                        UPPER(TRIM(cr.StudentOffenderName)) = UPPER(TRIM(s.StudentName))
                        AND UPPER(TRIM(cr.GradeLevel)) = UPPER(TRIM(s.GradeLevel))
                        AND UPPER(TRIM(cr.Section)) = UPPER(TRIM(s.Section))
                    WHERE (cr.IsActive = 1 OR cr.IsActive IS NULL)
                        AND UPPER(TRIM(cr.LevelOfOffense)) = 'MINOR'
                        AND s.TeacherID = @TeacherID
                        AND s.IsActive = 1";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND cr.Status = @Status";
                }

                query += " ORDER BY cr.DateOfOffense DESC LIMIT @Offset, @PageSize";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherID", teacherId);
                command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                _logger.LogInformation("Executing query: {Query}", query);
                _logger.LogInformation("Query parameters - TeacherID: {TeacherID}, Offset: {Offset}, PageSize: {PageSize}, Status: {Status}", 
                    teacherId, (page - 1) * pageSize, pageSize, status ?? "all");

                // Debug: Check if there are any students for this teacher
                var studentCountQuery = "SELECT COUNT(*) FROM students WHERE TeacherID = @TeacherID AND IsActive = 1";
                using var studentCountCommand = new MySqlCommand(studentCountQuery, connection);
                studentCountCommand.Parameters.AddWithValue("@TeacherID", teacherId);
                var studentCount = await studentCountCommand.ExecuteScalarAsync();
                _logger.LogInformation("Total students for teacher {TeacherID}: {StudentCount}", teacherId, studentCount);

                // Debug: Check if there are any minor case records
                var caseCountQuery = "SELECT COUNT(*) FROM studentprofilecaserecords WHERE (IsActive = 1 OR IsActive IS NULL) AND UPPER(TRIM(LevelOfOffense)) = 'MINOR'";
                using var caseCountCommand = new MySqlCommand(caseCountQuery, connection);
                var caseCount = await caseCountCommand.ExecuteScalarAsync();
                _logger.LogInformation("Total minor case records in database: {CaseCount}", caseCount);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var record = new StudentProfileCaseRecordSummary
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        StudentOffenderName = reader.GetString("StudentOffenderName"),
                        GradeLevel = reader.GetString("GradeLevel"),
                        Section = reader.GetString("Section"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        LevelOfOffense = reader.GetString("LevelOfOffense"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        Status = reader.GetString("Status")
                    };
                    records.Add(record);
                }

                _logger.LogInformation("Retrieved {Count} minor case records for teacher {TeacherID}", records.Count, teacherId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting minor case records for teacher {TeacherID}: {Message}\nStack trace: {StackTrace}", teacherId, ex.Message, ex.StackTrace);
                throw;
            }

            return records;
        }

        public async Task<int> CountStudentOffensesAsync(string studentName)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*) 
                    FROM studentprofilecaserecords 
                    WHERE UPPER(TRIM(StudentOffenderName)) = UPPER(TRIM(@StudentName))
                      AND (IsActive = 1 OR IsActive IS NULL)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentName", studentName.Trim());

                var result = await command.ExecuteScalarAsync();
                var count = result != null ? Convert.ToInt32(result) : 0;

                _logger.LogInformation("Student {StudentName} has {Count} existing offense records", studentName, count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting offenses for student {StudentName}", studentName);
                return 0; // Default to 0 if error occurs
            }
        }

        public async Task<List<StudentProfileCaseRecordSummary>> GetCaseRecordsByStudentNameAsync(string studentName)
        {
            var records = new List<StudentProfileCaseRecordSummary>();

            try
            {
                _logger.LogInformation("Getting case records for student: {StudentName}", studentName);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var trimmedName = studentName.Trim();
                
                // Try to extract first and last name parts if available
                var nameParts = trimmedName.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[0].Trim() : trimmedName;
                var lastName = nameParts.Length > 1 ? nameParts[nameParts.Length - 1].Trim() : string.Empty;

                // Use flexible LIKE matching to handle various name formats:
                // - Exact match
                // - Partial matches (first name only, last name only)
                // - Reversed format (Last, First vs First Last)
                // - Name contains the search term
                var query = @"
                    SELECT RecordID, IncidentID, StudentOffenderName, GradeLevel, Section, ViolationCommitted, LevelOfOffense, DateOfOffense, Status, DetailsOfAgreement,
                           (SELECT Gender FROM students WHERE UPPER(TRIM(StudentName)) = UPPER(TRIM(studentprofilecaserecords.StudentOffenderName)) LIMIT 1) as Sex
                    FROM studentprofilecaserecords
                    WHERE (
                        -- Exact match
                        UPPER(TRIM(StudentOffenderName)) = UPPER(TRIM(@StudentName))
                        -- Name starts with search term
                        OR UPPER(TRIM(StudentOffenderName)) LIKE CONCAT(UPPER(TRIM(@StudentName)), ' %')
                        -- Name contains search term in the middle
                        OR UPPER(TRIM(StudentOffenderName)) LIKE CONCAT('% ', UPPER(TRIM(@StudentName)), ' %')
                        -- Name ends with search term
                        OR UPPER(TRIM(StudentOffenderName)) LIKE CONCAT('% ', UPPER(TRIM(@StudentName)))
                        -- Name contains search term anywhere
                        OR UPPER(TRIM(StudentOffenderName)) LIKE CONCAT('%', UPPER(TRIM(@StudentName)), '%')
                        -- Reversed format matching (Last, First)
                        OR (UPPER(REPLACE(REPLACE(REPLACE(REPLACE(TRIM(StudentOffenderName), ',', ''), '.', ''), '  ', ' '), ' ', '')) = 
                            UPPER(REPLACE(REPLACE(REPLACE(REPLACE(TRIM(@StudentName), ',', ''), '.', ''), '  ', ' '), ' ', '')))
                        -- First name match
                        OR (@FirstName != '' AND (
                            UPPER(TRIM(StudentOffenderName)) LIKE CONCAT(UPPER(TRIM(@FirstName)), '%')
                            OR UPPER(TRIM(StudentOffenderName)) LIKE CONCAT('% ', UPPER(TRIM(@FirstName)), '%')
                        ))
                        -- Last name match
                        OR (@LastName != '' AND (
                            UPPER(TRIM(StudentOffenderName)) LIKE CONCAT('%', UPPER(TRIM(@LastName)))
                            OR UPPER(TRIM(StudentOffenderName)) LIKE CONCAT('%', UPPER(TRIM(@LastName)), '%')
                        ))
                    )
                    AND (IsActive = 1 OR IsActive IS NULL)
                    ORDER BY 
                        -- Prioritize exact matches first
                        CASE WHEN UPPER(TRIM(StudentOffenderName)) = UPPER(TRIM(@StudentName)) THEN 0 ELSE 1 END,
                        DateOfOffense DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentName", trimmedName);
                command.Parameters.AddWithValue("@FirstName", firstName);
                command.Parameters.AddWithValue("@LastName", lastName);

                _logger.LogInformation("Executing query with student name: {StudentName}, firstName: {FirstName}, lastName: {LastName}", 
                    trimmedName, firstName, lastName);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var record = new StudentProfileCaseRecordSummary
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        StudentOffenderName = reader.GetString("StudentOffenderName"),
                        GradeLevel = reader.GetString("GradeLevel"),
                        Section = reader.GetString("Section"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        LevelOfOffense = reader.GetString("LevelOfOffense"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        Status = reader.GetString("Status"),
                        DetailsOfAgreement = reader.IsDBNull("DetailsOfAgreement") ? "" : reader.GetString("DetailsOfAgreement")
                    };
                    records.Add(record);
                    _logger.LogInformation("Found case record - ID: {RecordID}, Student: {StudentName}, Violation: {Violation}", 
                        record.RecordID, record.StudentOffenderName, record.ViolationCommitted);
                }

                _logger.LogInformation("Retrieved {Count} case records for student {StudentName}", records.Count, studentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records for student {StudentName}: {Message}\nStack trace: {StackTrace}", studentName, ex.Message, ex.StackTrace);
                throw;
            }

            return records;
        }

        public async Task<List<StudentProfileCaseRecordSummary>> GetCaseRecordsByUsernameAsync(string username)
        {
            var records = new List<StudentProfileCaseRecordSummary>();

            try
            {
                _logger.LogInformation("Getting case records for username: {Username}", username);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Join Users and Students tables to get the actual StudentName from username
                var query = @"
                    SELECT DISTINCT cr.RecordID, cr.IncidentID, cr.StudentOffenderName, cr.GradeLevel, cr.Section, 
                           cr.ViolationCommitted, cr.LevelOfOffense, cr.DateOfOffense, cr.Status, cr.DetailsOfAgreement
                    FROM studentprofilecaserecords cr
                    INNER JOIN students s ON 
                        (
                            -- Match by student name (case-insensitive, trimmed)
                            UPPER(TRIM(cr.StudentOffenderName)) = UPPER(TRIM(s.StudentName))
                            -- Or match if student name is contained in offender name or vice versa
                            OR UPPER(TRIM(cr.StudentOffenderName)) LIKE CONCAT('%', UPPER(TRIM(s.StudentName)), '%')
                            OR UPPER(TRIM(s.StudentName)) LIKE CONCAT('%', UPPER(TRIM(cr.StudentOffenderName)), '%')
                        )
                    INNER JOIN users u ON s.UserID = u.UserID
                    WHERE u.Username = @Username
                      AND s.IsActive = 1
                      AND (cr.IsActive = 1 OR cr.IsActive IS NULL)
                    ORDER BY cr.DateOfOffense DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username.Trim());

                _logger.LogInformation("Executing query with username: {Username}", username.Trim());

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var record = new StudentProfileCaseRecordSummary
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        StudentOffenderName = reader.GetString("StudentOffenderName"),
                        GradeLevel = reader.GetString("GradeLevel"),
                        Section = reader.GetString("Section"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        LevelOfOffense = reader.GetString("LevelOfOffense"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        Status = reader.GetString("Status"),
                        DetailsOfAgreement = reader.IsDBNull("DetailsOfAgreement") ? "" : reader.GetString("DetailsOfAgreement")
                    };
                    records.Add(record);
                    _logger.LogInformation("Found case record - ID: {RecordID}, Student: {StudentName}, Violation: {Violation}", 
                        record.RecordID, record.StudentOffenderName, record.ViolationCommitted);
                }

                _logger.LogInformation("Retrieved {Count} case records for username {Username}", records.Count, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records for username {Username}: {Message}\nStack trace: {StackTrace}", username, ex.Message, ex.StackTrace);
                throw;
            }

            return records;
        }

        public async Task<int> CreateCaseRecordAsync(StudentProfileCaseRecordRequest request)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO studentprofilecaserecords (
                        IncidentID, StudentOffenderName, GradeLevel, TrackStrand, Section,
                        AdviserName, FathersName, MothersName, ParentGuardianName, ParentGuardianContact,
                        DateOfOffense, LevelOfOffense, ViolationCommitted, OtherViolationDescription,
                        NumberOfOffense, DetailsOfAgreement, PODInCharge,
                        EvidencePhotoBase64, SignatureBase64,
                        Status, IsActive, CreatedBy, UpdatedBy
                    ) VALUES (
                        @IncidentID, @StudentOffenderName, @GradeLevel, @TrackStrand, @Section,
                        @AdviserName, @FathersName, @MothersName, @ParentGuardianName, @ParentGuardianContact,
                        @DateOfOffense, @LevelOfOffense, @ViolationCommitted, @OtherViolationDescription,
                        @NumberOfOffense, @DetailsOfAgreement, @PODInCharge,
                        @EvidencePhotoBase64, @SignatureBase64,
                        @Status, 1, @CreatedBy, @CreatedBy
                    )";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@IncidentID", request.IncidentID.HasValue ? (object)request.IncidentID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@StudentOffenderName", request.StudentOffenderName);
                command.Parameters.AddWithValue("@GradeLevel", request.GradeLevel);
                command.Parameters.AddWithValue("@TrackStrand", request.TrackStrand ?? string.Empty);
                command.Parameters.AddWithValue("@Section", request.Section);
                // Set ParentGuardianName - if GuardianName is empty but database requires NOT NULL, use empty string
                var parentGuardianNameValue = request.GuardianName;
                if (string.IsNullOrWhiteSpace(parentGuardianNameValue))
                {
                    parentGuardianNameValue = string.Empty; // Use empty string instead of null for NOT NULL constraint
                }

                command.Parameters.AddWithValue("@AdviserName", request.AdviserName);
                command.Parameters.AddWithValue("@FathersName", request.FathersName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MothersName", request.MothersName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ParentGuardianName", parentGuardianNameValue);
                command.Parameters.AddWithValue("@ParentGuardianContact", request.ParentGuardianContact);
                command.Parameters.AddWithValue("@DateOfOffense", request.DateOfOffense);
                command.Parameters.AddWithValue("@LevelOfOffense", request.LevelOfOffense);
                command.Parameters.AddWithValue("@ViolationCommitted", request.ViolationCommitted);
                command.Parameters.AddWithValue("@OtherViolationDescription", request.OtherViolationDescription ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@NumberOfOffense", request.NumberOfOffense);
                command.Parameters.AddWithValue("@DetailsOfAgreement", request.DetailsOfAgreement);
                command.Parameters.AddWithValue("@PODInCharge", request.PODInCharge ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@EvidencePhotoBase64", request.EvidencePhotoBase64 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SignatureBase64", request.SignatureBase64 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", request.Status);
                command.Parameters.AddWithValue("@CreatedBy", 1); // This should come from authentication

                await command.ExecuteNonQueryAsync();
                return (int)command.LastInsertedId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating case record: {Message}\nStack trace: {StackTrace}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> UpdateCaseRecordAsync(int recordId, StudentProfileCaseRecordModel request)
        {   
            try
            {
                _logger.LogInformation("Updating case record {RecordId}", recordId);
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    UPDATE studentprofilecaserecords 
                    SET StudentOffenderName = @StudentOffenderName,
                        GradeLevel = @GradeLevel,
                        TrackStrand = @TrackStrand,
                        Section = @Section,
                        AdviserName = @AdviserName,
                        FathersName = @FathersName,
                        MothersName = @MothersName,
                        ParentGuardianName = @ParentGuardianName,
                        ParentGuardianContact = @ParentGuardianContact,
                        DateOfOffense = @DateOfOffense,
                        LevelOfOffense = @LevelOfOffense,
                        ViolationCommitted = @ViolationCommitted,
                        OtherViolationDescription = @OtherViolationDescription,
                        NumberOfOffense = @NumberOfOffense,
                        DetailsOfAgreement = @DetailsOfAgreement,
                        PODInCharge = @PODInCharge,
                        Status = @Status
                    WHERE RecordID = @RecordID AND (IsActive = 1 OR IsActive IS NULL)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@RecordID", recordId);
                command.Parameters.AddWithValue("@StudentOffenderName", request.StudentOffenderName);
                command.Parameters.AddWithValue("@GradeLevel", request.GradeLevel);
                command.Parameters.AddWithValue("@TrackStrand", request.TrackStrand ?? string.Empty);
                command.Parameters.AddWithValue("@Section", request.Section);
                command.Parameters.AddWithValue("@AdviserName", request.AdviserName);
                command.Parameters.AddWithValue("@FathersName", request.FathersName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MothersName", request.MothersName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ParentGuardianName", request.GuardianName ?? string.Empty);
                command.Parameters.AddWithValue("@ParentGuardianContact", request.ParentGuardianContact);
                command.Parameters.AddWithValue("@DateOfOffense", request.DateOfOffense);
                command.Parameters.AddWithValue("@LevelOfOffense", request.LevelOfOffense);
                command.Parameters.AddWithValue("@ViolationCommitted", request.ViolationCommitted);
                command.Parameters.AddWithValue("@OtherViolationDescription", request.OtherViolationDescription ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@NumberOfOffense", request.NumberOfOffense);
                command.Parameters.AddWithValue("@DetailsOfAgreement", request.DetailsOfAgreement);
                command.Parameters.AddWithValue("@PODInCharge", request.PODInCharge ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", request.Status);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Update affected {RowsAffected} rows for RecordID {RecordId}", rowsAffected, recordId);
                
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating case record {RecordId}: {Message}\nStack trace: {StackTrace}", recordId, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<StudentProfileCaseRecordModel> GetCaseRecordByIdAsync(int recordId)
        {
            try
            {
                _logger.LogInformation("Fetching case record with ID: {RecordId}", recordId);
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Join with Students table to get SchoolName and full StudentName
                var query = @"
                    SELECT cr.*, 
                           cr.EvidencePhotoBase64, cr.SignatureBase64,
                           COALESCE(sc.SchoolName, s.SchoolName) AS SchoolName,
                           sc.Region, sc.Division, sc.District,
                           s.StudentName AS FullStudentName
                    FROM studentprofilecaserecords cr
                    LEFT JOIN students s ON 
                        (UPPER(TRIM(cr.StudentOffenderName)) = UPPER(TRIM(s.StudentName)) 
                         OR UPPER(TRIM(cr.StudentOffenderName)) LIKE CONCAT('%', UPPER(TRIM(s.StudentName)), '%')
                         OR UPPER(TRIM(s.StudentName)) LIKE CONCAT('%', UPPER(TRIM(cr.StudentOffenderName)), '%'))
                        AND s.IsActive = 1
                    LEFT JOIN schools sc ON s.SchoolID = sc.SchoolID
                    WHERE cr.RecordID = @RecordID AND (cr.IsActive = 1 OR cr.IsActive IS NULL)
                    ORDER BY 
                        -- Prioritize exact match
                        CASE WHEN UPPER(TRIM(cr.StudentOffenderName)) = UPPER(TRIM(s.StudentName)) THEN 0 ELSE 1 END
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@RecordID", recordId);

                _logger.LogInformation("Executing query for RecordID: {RecordId}", recordId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    _logger.LogInformation("Found case record with ID: {RecordId}", recordId);
                    
                    // Use FullStudentName from Students table if available, otherwise fallback to record's name
                    var fullStudentName = reader.IsDBNull("FullStudentName") ? reader.GetString("StudentOffenderName") : reader.GetString("FullStudentName");
                    var schoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName");
                    
                    return new StudentProfileCaseRecordModel
                    {
                        StudentOffenderName = fullStudentName, // Use full name here
                        GradeLevel = reader.GetString("GradeLevel"),
                        TrackStrand = reader.GetString("TrackStrand"),
                        Section = reader.GetString("Section"),
                        AdviserName = reader.GetString("AdviserName"),
                        FathersName = reader.IsDBNull("FathersName") ? null : reader.GetString("FathersName"),
                        MothersName = reader.IsDBNull("MothersName") ? null : reader.GetString("MothersName"),
                        GuardianName = reader.IsDBNull("ParentGuardianName") ? null : reader.GetString("ParentGuardianName"),
                        ParentGuardianContact = reader.GetString("ParentGuardianContact"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        LevelOfOffense = reader.GetString("LevelOfOffense"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        OtherViolationDescription = reader.IsDBNull("OtherViolationDescription") ? null : reader.GetString("OtherViolationDescription"),
                        NumberOfOffense = reader.GetString("NumberOfOffense"),
                        DetailsOfAgreement = reader.GetString("DetailsOfAgreement"),
                        PODInCharge = reader.IsDBNull("PODInCharge") ? null : reader.GetString("PODInCharge"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        Status = reader.GetString("Status"),
                        SchoolName = schoolName, // Populate SchoolName
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        SignatureBase64 = reader.IsDBNull("SignatureBase64") ? null : reader.GetString("SignatureBase64")
                    };
                }
                else
                {
                    _logger.LogWarning("No case record found with ID: {RecordId}", recordId);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case record by ID {RecordId}: {Message}", recordId, ex.Message);
                return null;
            }
        }

        private async Task<string?> ResolveAdviserNameAsync(MySqlConnection connection, int? teacherId, string? gradeLevel, string? section, string? schoolName)
        {
            try
            {
                if (teacherId.HasValue && teacherId.Value > 0)
                {
                    var adviserQuery = @"
                        SELECT TeacherName
                        FROM teachers
                        WHERE TeacherID = @TeacherID
                          AND IsActive = 1
                        LIMIT 1";

                    await using var adviserCommand = new MySqlCommand(adviserQuery, connection);
                    adviserCommand.Parameters.AddWithValue("@TeacherID", teacherId.Value);

                    var adviserById = await adviserCommand.ExecuteScalarAsync() as string;
                    if (!string.IsNullOrWhiteSpace(adviserById))
                    {
                        _logger.LogInformation("Found adviser via TeacherID {TeacherID}: {AdviserName}", teacherId, adviserById);
                        return adviserById;
                    }
                }

                var fallbackQuery = @"
                    SELECT TeacherName
                    FROM teachers
                    WHERE IsActive = 1
                      AND UPPER(TRIM(Position)) IN ('ADVISER', 'CLASS ADVISER', 'CLASSADVISER')
                      AND (@GradeLevel IS NULL OR UPPER(TRIM(GradeLevel)) = UPPER(TRIM(@GradeLevel)))
                      AND (@Section IS NULL OR UPPER(TRIM(Section)) = UPPER(TRIM(@Section)))
                      AND (@SchoolName IS NULL OR TRIM(UPPER(COALESCE(SchoolName, ''))) = TRIM(UPPER(@SchoolName)))
                    ORDER BY TeacherName
                    LIMIT 1";

                await using var fallbackCommand = new MySqlCommand(fallbackQuery, connection);
                fallbackCommand.Parameters.AddWithValue("@GradeLevel", (object?)gradeLevel ?? DBNull.Value);
                fallbackCommand.Parameters.AddWithValue("@Section", (object?)section ?? DBNull.Value);
                fallbackCommand.Parameters.AddWithValue("@SchoolName", (object?)schoolName ?? DBNull.Value);

                var adviserFallback = await fallbackCommand.ExecuteScalarAsync() as string;
                if (!string.IsNullOrWhiteSpace(adviserFallback))
                {
                    _logger.LogInformation("Found adviser via fallback (Grade: {GradeLevel}, Section: {Section}, School: {SchoolName}): {AdviserName}",
                        gradeLevel ?? "ANY", section ?? "ANY", schoolName ?? "ANY", adviserFallback);
                    return adviserFallback;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving adviser name: {Message}", ex.Message);
            }

            return null;
        }

        public async Task<List<StudentAdviserInfo>> GetIncidentReportStudentInfoAsync(string studentName, int? incidentId = null)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // First try to get student data directly from Students table
                var studentDataQuery = @"
                    SELECT *
                    FROM students s
                    LEFT JOIN schools sc ON s.SchoolID = sc.SchoolID
                    WHERE s.IsActive = 1
                      AND (
                          UPPER(TRIM(s.StudentName)) = UPPER(TRIM(@StudentName))
                          OR UPPER(REPLACE(REPLACE(REPLACE(s.StudentName, ',', ''), '.', ''), ' ', '')) = 
                             UPPER(REPLACE(REPLACE(REPLACE(@StudentName, ',', ''), '.', ''), ' ', ''))
                          OR UPPER(TRIM(s.StudentName)) LIKE CONCAT('%', UPPER(TRIM(@StudentName)), '%')
                      )
                    ORDER BY s.DateRegister DESC
                    LIMIT 1";

                using var studentCommand = new MySqlCommand(studentDataQuery, connection);
                studentCommand.Parameters.AddWithValue("@StudentName", studentName);

                string? gradeLevel = null;
                string? section = null;
                string? trackStrand = null;
                string? schoolName = null;
                int? teacherId = null;
                DateTime? studentRegisterDate = null;
                string? sex = null;
                DateTime? birthDate = null;
                string? address = null;

                using (var studentReader = await studentCommand.ExecuteReaderAsync())
                {
                    if (await studentReader.ReadAsync())
                    {
                        gradeLevel = studentReader.IsDBNull("GradeLevel") ? null : studentReader.GetString("GradeLevel");
                        section = studentReader.IsDBNull("Section") ? null : studentReader.GetString("Section");
                        
                        // Robustly try to find any column that might represent Track/Strand
                        try {
                            int strandOrdinal = -1;
                            try { strandOrdinal = studentReader.GetOrdinal("Strand"); } catch { }
                            
                            int trackStrandOrdinal = -1;
                            try { trackStrandOrdinal = studentReader.GetOrdinal("TrackStrand"); } catch { }

                            if (strandOrdinal != -1 && !studentReader.IsDBNull(strandOrdinal))
                            {
                                trackStrand = studentReader.GetString(strandOrdinal);
                            }
                            else if (trackStrandOrdinal != -1 && !studentReader.IsDBNull(trackStrandOrdinal))
                            {
                                trackStrand = studentReader.GetString(trackStrandOrdinal);
                            }
                        } catch { }

                        schoolName = studentReader.IsDBNull("SchoolName") ? null : studentReader.GetString("SchoolName");
                        teacherId = studentReader.IsDBNull("TeacherID") ? (int?)null : studentReader.GetInt32("TeacherID");
                        studentRegisterDate = studentReader.IsDBNull("DateRegister") ? (DateTime?)null : studentReader.GetDateTime("DateRegister");
                        sex = studentReader.IsDBNull("Gender") ? null : studentReader.GetString("Gender");
                        birthDate = studentReader.IsDBNull("BirthDate") ? (DateTime?)null : studentReader.GetDateTime("BirthDate");
                        address = studentReader.IsDBNull("Address") ? null : studentReader.GetString("Address");
                        
                        _logger.LogInformation("Found student data for {StudentName}: Grade={Grade}, Section={Section}, Strand={Strand}", 
                            studentName, gradeLevel, section, trackStrand);
                    }
                    else
                    {
                        _logger.LogWarning("No student record found in Students table for {StudentName}. Trying historical fallback...", studentName);
                        var historical = await GetGradeSectionFromCaseRecordsAsync(connection, studentName);
                        if (historical.HasValue)
                        {
                            gradeLevel = historical.Value.GradeLevel;
                            section = historical.Value.Section;
                            trackStrand = historical.Value.TrackStrand;
                            _logger.LogInformation("Historical fallback found for {Name}: Grade={Grade}, Strand={Strand}", studentName, gradeLevel, trackStrand);
                        }
                    }
                }

                // Now get incident report data
                var query = @"
                    SELECT ir.IncidentID, ir.RespondentName, ir.AdviserName, ir.DateReported
                    FROM incidentreports ir
                    WHERE UPPER(TRIM(ir.RespondentName)) = UPPER(TRIM(@StudentName))
                      AND ir.IsActive = 1";

                if (incidentId.HasValue)
                {
                    query += " AND ir.IncidentID = @IncidentId";
                }

                query += @"
                    ORDER BY ir.DateReported DESC
                    LIMIT 5";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentName", studentName);
                if (incidentId.HasValue)
                {
                    command.Parameters.AddWithValue("@IncidentId", incidentId.Value);
                }

                var matches = new List<StudentAdviserInfo>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var adviserName = reader.IsDBNull("AdviserName") ? null : reader.GetString("AdviserName");

                    // If no adviser in incident report, try to resolve from student's grade/section
                    if (string.IsNullOrWhiteSpace(adviserName) && !string.IsNullOrWhiteSpace(gradeLevel))
                    {
                        adviserName = await ResolveAdviserNameAsync(connection, teacherId, gradeLevel, section, schoolName);
                    }

                    matches.Add(new StudentAdviserInfo
                    {
                        StudentId = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        StudentName = reader.IsDBNull("RespondentName") ? null : reader.GetString("RespondentName"),
                        AdviserName = adviserName,
                        GradeLevel = gradeLevel,  // Use data from Students table
                        Section = section,        // Use data from Students table
                        TrackStrand = trackStrand, // Use data from Students table
                        SchoolName = schoolName,   // Use data from Students table
                        TeacherId = teacherId,
                        Source = "Incident Reports",
                        Timestamp = reader.IsDBNull("DateReported") ? (DateTime?)null : reader.GetDateTime("DateReported"),
                        Sex = sex,
                        BirthDate = birthDate,
                        Address = address
                    });
                }

                return matches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident report student info for {StudentName}: {Message}", studentName, ex.Message);
                return new List<StudentAdviserInfo>();
            }
        }

        private async Task<(string? GradeLevel, string? Section, string? TrackStrand)?> GetGradeSectionFromCaseRecordsAsync(MySqlConnection connection, string studentName)
        {
            try
            {
                // Try StudentProfileCaseRecords first
                var query1 = @"
                    SELECT GradeLevel, Section, TrackStrand, DateReported
                    FROM studentprofilecaserecords
                    WHERE UPPER(TRIM(StudentOffenderName)) = UPPER(TRIM(@StudentName))
                    ORDER BY DateReported DESC
                    LIMIT 1";

                using var cmd1 = new MySqlCommand(query1, connection);
                cmd1.Parameters.AddWithValue("@StudentName", studentName);
                
                using (var reader1 = await cmd1.ExecuteReaderAsync())
                {
                    if (await reader1.ReadAsync())
                    {
                        var info = (
                            reader1.IsDBNull(0) ? null : reader1.GetString(0),
                            reader1.IsDBNull(1) ? null : reader1.GetString(1),
                            reader1.IsDBNull(2) ? null : reader1.GetString(2)
                        );
                        _logger.LogInformation("Found historical data in StudentProfileCaseRecords for {Name}: Grade={Grade}, Strand={Strand}", studentName, info.Item1, info.Item3);
                        return info;
                    }
                }

                // Try SimplifiedStudentProfileCaseRecords if not found or if we want the absolute latest
                var query2 = @"
                    SELECT GradeLevel, Section, TrackStrand, DateCreated
                    FROM simplifiedstudentprofilecaserecords
                    WHERE UPPER(TRIM(RespondentName)) = UPPER(TRIM(@StudentName))
                    ORDER BY DateCreated DESC
                    LIMIT 1";

                using var cmd2 = new MySqlCommand(query2, connection);
                cmd2.Parameters.AddWithValue("@StudentName", studentName);
                
                using (var reader2 = await cmd2.ExecuteReaderAsync())
                {
                    if (await reader2.ReadAsync())
                    {
                        var info = (
                            reader2.IsDBNull(0) ? null : reader2.GetString(0),
                            reader2.IsDBNull(1) ? null : reader2.GetString(1),
                            reader2.IsDBNull(2) ? null : reader2.GetString(2)
                        );
                        _logger.LogInformation("Found historical data in SimplifiedStudentProfileCaseRecords for {Name}: Grade={Grade}, Strand={Strand}", studentName, info.Item1, info.Item3);
                        return info;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error in GetGradeSectionFromCaseRecordsAsync: {Message}", ex.Message);
            }

            return null;
        }


        /// <summary>
        /// Gets case records for POD with routing logic:
        /// - Major and Prohibited Acts cases are returned immediately
        /// - Students with 3+ minor cases get ONE consolidated record showing all minor violations
        /// - Students with 1-2 minor cases are NOT visible to POD
        /// </summary>
        public async Task<List<StudentProfileCaseRecordSummary>> GetCaseRecordsForPODAsync(int? schoolId = null, string? status = null, int page = 1, int pageSize = 100)
        {
            var records = new List<StudentProfileCaseRecordSummary>();

            try
            {
                _logger.LogInformation("Getting case records for POD with routing logic. SchoolID: {SchoolID}, Status: {Status}", schoolId, status);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Step 1: Get all Major and Prohibited Acts cases (these go directly to POD)
                var majorProhibitedQuery = @"
                    SELECT cr.RecordID, cr.IncidentID, cr.StudentOffenderName, cr.GradeLevel, cr.Section, 
                           cr.ViolationCommitted, cr.LevelOfOffense, cr.DateOfOffense, cr.Status, s.Gender as Sex
                    FROM studentprofilecaserecords cr
                    LEFT JOIN students s ON 
                        UPPER(TRIM(cr.StudentOffenderName)) = UPPER(TRIM(s.StudentName))
                        AND UPPER(TRIM(cr.GradeLevel)) = UPPER(TRIM(s.GradeLevel))
                        AND UPPER(TRIM(cr.Section)) = UPPER(TRIM(s.Section))
                    WHERE (cr.IsActive = 1 OR cr.IsActive IS NULL)
                      AND (UPPER(TRIM(LevelOfOffense)) = 'MAJOR' 
                           OR UPPER(TRIM(LevelOfOffense)) = 'PROHIBITED ACTS'
                           OR UPPER(TRIM(LevelOfOffense)) = 'PROHIBITED')";

                if (schoolId.HasValue)
                {
                    majorProhibitedQuery += " AND SchoolID = @SchoolID";
                }

                if (!string.IsNullOrEmpty(status))
                {
                    majorProhibitedQuery += " AND Status = @Status";
                }

                majorProhibitedQuery += " ORDER BY DateOfOffense DESC";

                using var majorCommand = new MySqlCommand(majorProhibitedQuery, connection);
                if (schoolId.HasValue)
                {
                    majorCommand.Parameters.AddWithValue("@SchoolID", schoolId.Value);
                }
                if (!string.IsNullOrEmpty(status))
                {
                    majorCommand.Parameters.AddWithValue("@Status", status);
                }

                using var majorReader = await majorCommand.ExecuteReaderAsync();
                while (await majorReader.ReadAsync())
                {
                    records.Add(new StudentProfileCaseRecordSummary
                    {
                        RecordID = majorReader.GetInt32("RecordID"),
                        IncidentID = majorReader.IsDBNull("IncidentID") ? (int?)null : majorReader.GetInt32("IncidentID"),
                        StudentOffenderName = majorReader.GetString("StudentOffenderName"),
                        GradeLevel = majorReader.GetString("GradeLevel"),
                        Section = majorReader.GetString("Section"),
                        ViolationCommitted = majorReader.GetString("ViolationCommitted"),
                        LevelOfOffense = majorReader.GetString("LevelOfOffense"),
                        DateOfOffense = majorReader.GetDateTime("DateOfOffense"),
                        Status = majorReader.GetString("Status"),
                        Sex = majorReader.IsDBNull("Sex") ? "" : majorReader.GetString("Sex")
                    });
                }
                await majorReader.CloseAsync();

                _logger.LogInformation("Found {Count} Major/Prohibited cases for POD", records.Count);

                // Step 2: Get students with 3+ minor cases and create consolidated records
                var minorCasesQuery = @"
                    SELECT StudentOffenderName, GradeLevel, Section, SchoolID,
                           COUNT(*) as MinorCount,
                           GROUP_CONCAT(ViolationCommitted SEPARATOR '; ') as AllViolations,
                           MIN(DateOfOffense) as FirstOffense,
                           MAX(DateOfOffense) as LatestOffense,
                           MAX(Status) as Status
                    FROM studentprofilecaserecords
                    WHERE (IsActive = 1 OR IsActive IS NULL)
                      AND UPPER(TRIM(LevelOfOffense)) = 'MINOR'";

                if (schoolId.HasValue)
                {
                    minorCasesQuery += " AND SchoolID = @SchoolID";
                }

                minorCasesQuery += @"
                    GROUP BY StudentOffenderName, GradeLevel, Section, SchoolID
                    HAVING COUNT(*) >= 3";

                if (!string.IsNullOrEmpty(status))
                {
                    minorCasesQuery += " AND MAX(Status) = @Status";
                }

                using var minorCommand = new MySqlCommand(minorCasesQuery, connection);
                if (schoolId.HasValue)
                {
                    minorCommand.Parameters.AddWithValue("@SchoolID", schoolId.Value);
                }
                if (!string.IsNullOrEmpty(status))
                {
                    minorCommand.Parameters.AddWithValue("@Status", status);
                }

                using var minorReader = await minorCommand.ExecuteReaderAsync();
                while (await minorReader.ReadAsync())
                {
                    var minorCount = minorReader.GetInt32("MinorCount");
                    var studentName = minorReader.GetString("StudentOffenderName");
                    
                    // Create a consolidated record for this student
                    records.Add(new StudentProfileCaseRecordSummary
                    {
                        RecordID = -1, // Negative ID to indicate this is a consolidated record
                        IncidentID = null,
                        StudentOffenderName = studentName,
                        GradeLevel = minorReader.GetString("GradeLevel"),
                        Section = minorReader.GetString("Section"),
                        ViolationCommitted = $"Multiple Minor Offenses ({minorCount} cases)",
                        LevelOfOffense = "Minor (Consolidated)",
                        DateOfOffense = minorReader.GetDateTime("LatestOffense"),
                        Status = minorReader.GetString("Status"),
                        DetailsOfAgreement = $"Student has {minorCount} minor cases. Violations: {minorReader.GetString("AllViolations")}"
                    });
                }
                await minorReader.CloseAsync();

                _logger.LogInformation("Found {Count} students with 3+ minor cases for POD", records.Count(r => r.RecordID == -1));
                _logger.LogInformation("Total case records for POD: {TotalCount}", records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records for POD: {Message}", ex.Message);
                throw;
            }

            return records.OrderByDescending(r => r.DateOfOffense).ToList();
        }

        private static string NormalizeStudentName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var normalized = new string(name.Where(char.IsLetterOrDigit).ToArray());
            return normalized.ToUpperInvariant();
        }
        public async Task<List<int>> GetIncidentIdsWithCaseRecordsAsync(string? schoolName = null)
        {
            var incidentIds = new List<int>();
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Use SimplifiedStudentProfileCaseRecords table (the new table)
                var query = @"
                    SELECT DISTINCT IncidentID 
                    FROM simplifiedstudentprofilecaserecords 
                    WHERE (IsActive = 1 OR IsActive IS NULL) AND IncidentID IS NOT NULL";

                using var command = new MySqlCommand(query, connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    incidentIds.Add(reader.GetInt32(0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident IDs with case records");
            }
            return incidentIds;
        }
        public async Task<StudentProfileCaseRecordSummary?> GetCaseRecordByIncidentIdAsync(int incidentId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT cr.*, 
                           COALESCE(sc.SchoolName, s.SchoolName) AS SchoolName,
                           sc.Region, sc.Division, sc.District
                    FROM studentprofilecaserecords cr
                    LEFT JOIN students s ON 
                        (UPPER(TRIM(cr.StudentOffenderName)) = UPPER(TRIM(s.StudentName)) 
                         OR UPPER(TRIM(cr.StudentOffenderName)) LIKE CONCAT('%', UPPER(TRIM(s.StudentName)), '%')
                         OR UPPER(TRIM(s.StudentName)) LIKE CONCAT('%', UPPER(TRIM(cr.StudentOffenderName)), '%'))
                        AND s.IsActive = 1
                    LEFT JOIN schools sc ON s.SchoolID = sc.SchoolID
                    WHERE cr.IncidentID = @IncidentID AND (cr.IsActive = 1 OR cr.IsActive IS NULL)
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@IncidentID", incidentId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new StudentProfileCaseRecordSummary
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        StudentOffenderName = reader.GetString("StudentOffenderName"),
                        GradeLevel = reader.GetString("GradeLevel"),
                        Section = reader.GetString("Section"),
                        ViolationCommitted = reader.IsDBNull("ViolationCommitted") ? "" : reader.GetString("ViolationCommitted"),
                        LevelOfOffense = reader.GetString("LevelOfOffense"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        Status = reader.GetString("Status"),
                        DetailsOfAgreement = reader.IsDBNull("DetailsOfAgreement") ? "" : reader.GetString("DetailsOfAgreement"),
                        NumberOfOffense = reader.IsDBNull("NumberOfOffense") ? "" : reader.GetString("NumberOfOffense"),
                        AdviserName = reader.IsDBNull("AdviserName") ? "" : reader.GetString("AdviserName"),
                        FathersName = reader.IsDBNull("FathersName") ? null : reader.GetString("FathersName"),
                        MothersName = reader.IsDBNull("MothersName") ? null : reader.GetString("MothersName"),
                        GuardianName = reader.IsDBNull("ParentGuardianName") ? null : reader.GetString("ParentGuardianName"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        SignatureBase64 = reader.IsDBNull("SignatureBase64") ? null : reader.GetString("SignatureBase64"),
                        ParentGuardianContact = reader.IsDBNull("ParentGuardianContact") ? null : reader.GetString("ParentGuardianContact")
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case record by incident ID: {IncidentId}", incidentId);
            }
            return null;
        }
        public async Task<int> CountCasesByStudentNameAsync(string studentName)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM studentprofilecaserecords WHERE StudentOffenderName = @StudentName AND (IsActive = 1 OR IsActive IS NULL)";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentName", studentName);

                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting cases for student: {StudentName}", studentName);
                return 1; // Default to 1 on error
            }
        }
        public async Task<int> CreateSimplifiedCaseRecordAsync(SimplifiedStudentProfileCaseRecordRequest request)
        {
            try
            {
                _logger.LogInformation("Creating simplified case record for respondent: {RespondentName}", request.RespondentName);
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO simplifiedstudentprofilecaserecords (
                        IncidentID, EscalationID, RespondentName, DateOfOffense, ViolationCommitted, 
                        ViolationCategory, Description, EvidencePhotoBase64, StudentSignatureBase64, 
                        DateOfBirth, Age, Sex, Address, GradeLevel, TrackStrand, Section, AdviserName, 
                        ActionTaken, Findings, Agreement, PenaltyAction,
                        Status, IsActive,
                        ParentContactType, ParentContactName, ParentMeetingDate,
                        FathersName, MothersName, GuardianName
                    ) VALUES (
                        @IncidentID, @EscalationID, @RespondentName, @DateOfOffense, @ViolationCommitted, 
                        @ViolationCategory, @Description, @EvidencePhotoBase64, @StudentSignatureBase64, 
                        @DateOfBirth, @Age, @Sex, @Address, @GradeLevel, @TrackStrand, @Section, @AdviserName, 
                        @ActionTaken, @Findings, @Agreement, @PenaltyAction,
                        @Status, 1,
                        @ParentContactType, @ParentContactName, @ParentMeetingDate,
                        @FathersName, @MothersName, @GuardianName
                    )";

                // Check if this is an escalation and fetch minor offenses if needed
                string violationCommitted = request.ViolationCommitted;
                string description = request.Description;
                string violationCategory = request.ViolationCategory; // Start with request value

                if (request.EscalationID.HasValue)
                {
                    // Force ViolationCategory to 'Major' for escalated cases (3+ minor offenses)
                    violationCategory = "Major";
                    
                    try 
                    {
                        var getMinorsQuery = @"SELECT IncidentType, DateReported 
                                              FROM simplifiedincidentreports 
                                              WHERE RespondentName = @RespondentName 
                                              AND IncidentType IN (SELECT ViolationName FROM violationtypes WHERE ViolationCategory = 'Minor')
                                              ORDER BY DateReported DESC
                                              LIMIT 5";
                                              
                        using var minorCmd = new MySqlCommand(getMinorsQuery, connection);
                        minorCmd.Parameters.AddWithValue("@RespondentName", request.RespondentName);
                        
                        using var reader = await minorCmd.ExecuteReaderAsync();
                        var minorOffenses = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            if (!reader.IsDBNull("IncidentType"))
                            {
                                minorOffenses.Add(reader.GetString("IncidentType"));
                            }
                        }
                        
                        if (minorOffenses.Any())
                        {
                            var aggregated = string.Join(", ", minorOffenses.Distinct());
                            // If the current violation is generic, append the specifics
                            if (string.IsNullOrEmpty(violationCommitted) || violationCommitted.Contains("Accumulated") || violationCommitted.Contains("Major"))
                            {
                                violationCommitted = $"Major Case (Accumulated: {aggregated})";
                            }
                            
                            // Also update description if needed
                            if (string.IsNullOrEmpty(description))
                            {
                                description = $"Automatically escalated due to multiple minor offenses: {aggregated}";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error aggregating minor offenses for escalation {EscalationID}", request.EscalationID);
                    }
                }

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@IncidentID", request.IncidentID.HasValue ? (object)request.IncidentID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@EscalationID", request.EscalationID.HasValue ? (object)request.EscalationID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@RespondentName", request.RespondentName);
                command.Parameters.AddWithValue("@DateOfOffense", request.DateOfOffense);
                command.Parameters.AddWithValue("@ViolationCommitted", violationCommitted);
                command.Parameters.AddWithValue("@ViolationCategory", violationCategory ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@EvidencePhotoBase64", request.EvidencePhotoBase64 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@StudentSignatureBase64", request.StudentSignatureBase64 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DateOfBirth", request.DateOfBirth ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Age", request.Age ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Sex", request.Sex ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Address", request.Address ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@GradeLevel", request.GradeLevel ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TrackStrand", request.TrackStrand ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Section", request.Section ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AdviserName", request.AdviserName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ActionTaken", request.ActionTaken ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Findings", request.Findings ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Agreement", request.Agreement ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PenaltyAction", request.PenaltyAction ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", request.Status ?? "Active");
                command.Parameters.AddWithValue("@ParentContactType", string.IsNullOrEmpty(request.ParentContactType) ? (object)DBNull.Value : request.ParentContactType);
                command.Parameters.AddWithValue("@ParentContactName", string.IsNullOrEmpty(request.ParentContactName) ? (object)DBNull.Value : request.ParentContactName);
                command.Parameters.AddWithValue("@ParentMeetingDate", request.ParentMeetingDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FathersName", request.FathersName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MothersName", request.MothersName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@GuardianName", request.GuardianName ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
                var recordId = (int)command.LastInsertedId;

                // Sync respondent name back to SimplifiedIncidentReports if IncidentID is provided
                if (request.IncidentID.HasValue)
                {
                    _logger.LogInformation("Syncing respondent name to SimplifiedIncidentReports for IncidentID: {IncidentID}", request.IncidentID.Value);
                    
                    // Get current RespondentName from incident report
                    var getIncidentQuery = "SELECT RespondentName FROM simplifiedincidentreports WHERE IncidentID = @IncidentID";
                    using var getCmd = new MySqlCommand(getIncidentQuery, connection);
                    getCmd.Parameters.AddWithValue("@IncidentID", request.IncidentID.Value);
                    
                    var currentRespondentName = await getCmd.ExecuteScalarAsync() as string;
                    
                    if (!string.IsNullOrEmpty(currentRespondentName))
                    {
                        // Split the current respondent names (semicolon or slash separated)
                        var respondents = currentRespondentName
                            .Split(new[] { ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(n => n.Trim())
                            .ToList();
                        
                        // Find and replace the old name with the new name (case-insensitive match)
                        var updated = false;
                        for (int i = 0; i < respondents.Count; i++)
                        {
                            if (string.Equals(respondents[i], request.RespondentName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Name already matches, no update needed
                                _logger.LogInformation("Respondent name already matches in incident report");
                                updated = true;
                                break;
                            }
                            
                            // Check if this is a close match (could be a typo being corrected)
                            // Match if the normalized names are similar
                            var normalizedOld = new string(respondents[i].Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
                            var normalizedNew = new string(request.RespondentName.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
                            
                            if (normalizedOld.Length > 0 && normalizedNew.Length > 0)
                            {
                                // If names are very similar (same length and many matching characters)
                                // or if one contains the other, consider it a match
                                if (normalizedOld == normalizedNew || 
                                    normalizedOld.Contains(normalizedNew) || 
                                    normalizedNew.Contains(normalizedOld))
                                {
                                    respondents[i] = request.RespondentName;
                                    updated = true;
                                    _logger.LogInformation("Updated respondent name from '{OldName}' to '{NewName}'", currentRespondentName, request.RespondentName);
                                    break;
                                }
                            }
                        }
                        
                        if (updated)
                        {
                            // Join the names back together with semicolon separator
                            var updatedRespondentName = string.Join("; ", respondents);
                            
                            // Update the incident report with the corrected name
                            var updateIncidentQuery = "UPDATE simplifiedincidentreports SET RespondentName = @RespondentName WHERE IncidentID = @IncidentID";
                            using var updateCmd = new MySqlCommand(updateIncidentQuery, connection);
                            updateCmd.Parameters.AddWithValue("@RespondentName", updatedRespondentName);
                            updateCmd.Parameters.AddWithValue("@IncidentID", request.IncidentID.Value);
                            await updateCmd.ExecuteNonQueryAsync();
                            
                            _logger.LogInformation("Successfully synced respondent name to incident report. New value: {RespondentName}", updatedRespondentName);
                        }
                    }
                }
                
                return recordId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating simplified case record: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> UpdateParentMeetingAsync(int recordId, string? parentContactType, string? parentContactName, DateTime? parentMeetingDate, string? status)
        {
            try
            {
                _logger.LogInformation("UpdateParentMeetingAsync called for RecordID: {RecordID}, ParentMeetingDate: {MeetingDate}, ParentContactType: {ContactType}, ParentContactName: {ContactName}, Status: {Status}", 
                    recordId, parentMeetingDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NULL", parentContactType ?? "NULL", parentContactName ?? "NULL", status ?? "NULL");

                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // 1. Update the Case Record
                var query = @"
                    UPDATE simplifiedstudentprofilecaserecords
                    SET ParentMeetingDate = @ParentMeetingDate,
                        ParentContactType = COALESCE(@ParentContactType, ParentContactType),
                        ParentContactName = COALESCE(@ParentContactName, ParentContactName),
                        Status = COALESCE(@Status, Status)
                    WHERE RecordID = @RecordID";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@RecordID", recordId);
                
                if (parentMeetingDate.HasValue)
                {
                    command.Parameters.AddWithValue("@ParentMeetingDate", parentMeetingDate.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@ParentMeetingDate", DBNull.Value);
                }
                
                command.Parameters.AddWithValue("@ParentContactType", string.IsNullOrEmpty(parentContactType) ? (object)DBNull.Value : parentContactType);
                command.Parameters.AddWithValue("@ParentContactName", string.IsNullOrEmpty(parentContactName) ? (object)DBNull.Value : parentContactName);
                command.Parameters.AddWithValue("@Status", string.IsNullOrEmpty(status) ? (object)DBNull.Value : status);

                var rows = await command.ExecuteNonQueryAsync();
                
                // 2. Sync to Incident Report and Escalation if Status Changed
                if (rows > 0 && !string.IsNullOrEmpty(status))
                {
                    // Get combined metadata to sync status correctly
                    var getMetadataQuery = @"
                        SELECT s.IncidentID, s.EscalationID, s.RespondentName, COALESCE(sir.SchoolName, ce.SchoolName) as SchoolName
                        FROM simplifiedstudentprofilecaserecords s
                        LEFT JOIN simplifiedincidentreports sir ON s.IncidentID = sir.IncidentID
                        LEFT JOIN caseescalations ce ON s.EscalationID = ce.EscalationID
                        WHERE s.RecordID = @RecordID";
                    using var getMetadataCmd = new MySqlCommand(getMetadataQuery, connection);
                    getMetadataCmd.Parameters.AddWithValue("@RecordID", recordId);
                    
                    string? respondentName = null;
                    string? schoolName = null;
                    int? incidentId = null;
                    int? escalationId = null;

                    using (var reader = await getMetadataCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            incidentId = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                            escalationId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                            respondentName = reader.IsDBNull(2) ? null : reader.GetString(2);
                            schoolName = reader.IsDBNull(3) ? null : reader.GetString(3);
                        }
                    }

                    // 2a. Sync to the specific linked Incident Report
                    if (incidentId.HasValue)
                    {
                        var updateIncidentQuery = "UPDATE simplifiedincidentreports SET Status = @Status WHERE IncidentID = @IncidentID";
                        using var updateIncCmd = new MySqlCommand(updateIncidentQuery, connection);
                        updateIncCmd.Parameters.AddWithValue("@Status", status);
                        updateIncCmd.Parameters.AddWithValue("@IncidentID", incidentId.Value);
                        await updateIncCmd.ExecuteNonQueryAsync();
                        _logger.LogInformation("Synced Status '{Status}' to specific IncidentID {IncidentID}", status, incidentId.Value);
                    }

                    // 2b. Sync to Escalation and ALL related minor incidents if this is an escalation record
                    if (escalationId.HasValue)
                    {
                        // Update Escalation status
                        var updateEscQuery = "UPDATE caseescalations SET Status = @Status WHERE EscalationID = @EscalationID";
                        using var updateEscCmd = new MySqlCommand(updateEscQuery, connection);
                        updateEscCmd.Parameters.AddWithValue("@Status", status);
                        updateEscCmd.Parameters.AddWithValue("@EscalationID", escalationId.Value);
                        await updateEscCmd.ExecuteNonQueryAsync();
                        _logger.LogInformation("Synced Status '{Status}' to EscalationID {EscalationID}", status, escalationId.Value);

                        // Update ALL other minor incidents for this student/school that are Pending or Approved
                        if (!string.IsNullOrEmpty(respondentName))
                        {
                            // We join with ViolationTypes to filter for 'Minor' offenses only
                            var syncMinorsQuery = @"
                                UPDATE simplifiedincidentreports sir
                                INNER JOIN violationtypes vt ON UPPER(TRIM(sir.IncidentType)) = UPPER(TRIM(vt.ViolationName))
                                SET sir.Status = @Status
                                WHERE sir.RespondentName = @Name 
                                AND sir.SchoolName = @School
                                AND vt.ViolationCategory = 'Minor'
                                AND sir.Status IN ('Pending', 'Approved', 'Reported', 'Calling', 'Arrived', 'UnderReview', 'ParentOnHold')";


                            using var syncMinorsCmd = new MySqlCommand(syncMinorsQuery, connection);
                            syncMinorsCmd.Parameters.AddWithValue("@Status", status);
                            syncMinorsCmd.Parameters.AddWithValue("@Name", respondentName);
                            syncMinorsCmd.Parameters.AddWithValue("@School", schoolName ?? "");
                            
                            var minorRows = await syncMinorsCmd.ExecuteNonQueryAsync();
                            if (minorRows > 0)
                            {
                                _logger.LogInformation("Broad-synced Status '{Status}' to {Count} related minor incidents for {Student}", status, minorRows, respondentName);
                            }
                        }
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating parent meeting for simplified case record {RecordID}: {Message}\nStack Trace: {StackTrace}", 
                    recordId, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<List<SimplifiedStudentProfileCaseRecordModel>> GetSimplifiedCaseRecordsAsync(
            string? respondentName = null, 
            string? status = null, 
            int? incidentId = null, 
            string? schoolName = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var records = new List<SimplifiedStudentProfileCaseRecordModel>();
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT c.*, i.VictimName, i.FullName as ComplainantName, i.ComplainantContactNumber as ComplainantContact, 
                           s.SchoolName, s.Region, s.Division, s.District
                    FROM simplifiedstudentprofilecaserecords c
                    LEFT JOIN simplifiedincidentreports i ON c.IncidentID = i.IncidentID
                    LEFT JOIN schools s ON i.SchoolName = s.SchoolName
                    WHERE c.IsActive = 1";

                if (!string.IsNullOrEmpty(respondentName))
                {
                    query += " AND c.RespondentName LIKE @RespondentName";
                }
                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND c.Status = @Status";
                }
                if (incidentId.HasValue)
                {
                    query += " AND c.IncidentID = @IncidentID";
                }
                if (!string.IsNullOrEmpty(schoolName))
                {
                    query += " AND s.SchoolName = @SchoolName";
                }
                if (startDate.HasValue)
                {
                    query += " AND (i.DateReported >= @StartDate OR (i.DateReported IS NULL AND c.DateCreated >= @StartDate))";
                }
                if (endDate.HasValue)
                {
                    // Add 1 day to end date to include the entire end day
                    query += " AND (i.DateReported <= @EndDate OR (i.DateReported IS NULL AND c.DateCreated <= @EndDate))";
                }

                query += " ORDER BY c.DateCreated DESC";

                using var command = new MySqlCommand(query, connection);
                if (!string.IsNullOrEmpty(respondentName))
                {
                    command.Parameters.AddWithValue("@RespondentName", $"%{respondentName}%");
                }
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }
                if (incidentId.HasValue)
                {
                    command.Parameters.AddWithValue("@IncidentID", incidentId.Value);
                }
                if (!string.IsNullOrEmpty(schoolName))
                {
                    command.Parameters.AddWithValue("@SchoolName", schoolName);
                }
                if (startDate.HasValue)
                {
                    command.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
                }
                if (endDate.HasValue)
                {
                    command.Parameters.AddWithValue("@EndDate", endDate.Value.Date.AddDays(1).AddSeconds(-1));
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    records.Add(new SimplifiedStudentProfileCaseRecordModel
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        EscalationID = reader.IsDBNull("EscalationID") ? (int?)null : reader.GetInt32("EscalationID"),
                        RespondentName = reader.GetString("RespondentName"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        ViolationCategory = reader.IsDBNull("ViolationCategory") ? null : reader.GetString("ViolationCategory"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        StudentSignatureBase64 = reader.IsDBNull("StudentSignatureBase64") ? null : reader.GetString("StudentSignatureBase64"),
                        DateOfBirth = reader.IsDBNull("DateOfBirth") ? (DateTime?)null : reader.GetDateTime("DateOfBirth"),
                        Age = reader.IsDBNull("Age") ? (int?)null : reader.GetInt32("Age"),
                        Sex = reader.IsDBNull("Sex") ? null : reader.GetString("Sex"),
                        Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel"),
                        TrackStrand = reader.IsDBNull("TrackStrand") ? null : reader.GetString("TrackStrand"),
                        Section = reader.IsDBNull("Section") ? null : reader.GetString("Section"),
                        AdviserName = reader.IsDBNull("AdviserName") ? null : reader.GetString("AdviserName"),
                        ActionTaken = reader.IsDBNull("ActionTaken") ? null : reader.GetString("ActionTaken"),
                        Findings = reader.IsDBNull("Findings") ? null : reader.GetString("Findings"),
                        Agreement = reader.IsDBNull("Agreement") ? null : reader.GetString("Agreement"),
                        PenaltyAction = reader.IsDBNull("PenaltyAction") ? null : reader.GetString("PenaltyAction"),
                        Status = reader.GetString("Status"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        ParentContactType = reader.IsDBNull("ParentContactType") ? null : reader.GetString("ParentContactType"),
                        ParentContactName = reader.IsDBNull("ParentContactName") ? null : reader.GetString("ParentContactName"),
                        ParentMeetingDate = reader.IsDBNull("ParentMeetingDate") ? (DateTime?)null : reader.GetDateTime("ParentMeetingDate"),
                        FathersName = reader.IsDBNull("FathersName") ? null : reader.GetString("FathersName"),
                        MothersName = reader.IsDBNull("MothersName") ? null : reader.GetString("MothersName"),
                        GuardianName = reader.IsDBNull("GuardianName") ? null : reader.GetString("GuardianName"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        VictimName = reader.IsDBNull("VictimName") ? null : reader.GetString("VictimName"),
                        ComplainantName = reader.IsDBNull("ComplainantName") ? null : reader.GetString("ComplainantName"),
                        ComplainantContact = reader.IsDBNull("ComplainantContact") ? null : reader.GetString("ComplainantContact")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting simplified case records");
            }
            return records;
        }

        public async Task<SimplifiedStudentProfileCaseRecordModel?> GetSimplifiedCaseRecordByEscalationIdAsync(int escalationId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // JOIN with SimplifiedIncidentReports and schools to get school info
                // Note: Even for escalations, we try to join by IncidentID column (if any)
                // but for now, we primarily filter by EscalationID
                var query = @"
                    SELECT s.*, 
                           sir.SchoolName as SchoolName,
                           sir.Division as Division,
                           sir.VictimName as VictimName,
                           sir.FullName as ComplainantName,
                           sir.ComplainantContactNumber as ComplainantContact,
                           sch.Region as Region,
                           sch.District as District
                    FROM simplifiedstudentprofilecaserecords s
                    LEFT JOIN simplifiedincidentreports sir ON s.IncidentID = sir.IncidentID
                    LEFT JOIN schools sch ON sir.SchoolName = sch.SchoolName
                    WHERE s.EscalationID = @EscalationID
                    ORDER BY s.DateCreated DESC
                    LIMIT 1";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@EscalationID", escalationId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SimplifiedStudentProfileCaseRecordModel
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        EscalationID = reader.IsDBNull("EscalationID") ? (int?)null : reader.GetInt32("EscalationID"),
                        RespondentName = reader.GetString("RespondentName"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        ViolationCategory = reader.IsDBNull("ViolationCategory") ? null : reader.GetString("ViolationCategory"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        StudentSignatureBase64 = reader.IsDBNull("StudentSignatureBase64") ? null : reader.GetString("StudentSignatureBase64"),
                        DateOfBirth = reader.IsDBNull("DateOfBirth") ? (DateTime?)null : reader.GetDateTime("DateOfBirth"),
                        Age = reader.IsDBNull("Age") ? (int?)null : reader.GetInt32("Age"),
                        Sex = reader.IsDBNull("Sex") ? null : reader.GetString("Sex"),
                        Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel"),
                        TrackStrand = reader.IsDBNull("TrackStrand") ? null : reader.GetString("TrackStrand"),
                        Section = reader.IsDBNull("Section") ? null : reader.GetString("Section"),
                        AdviserName = reader.IsDBNull("AdviserName") ? null : reader.GetString("AdviserName"),
                        ActionTaken = reader.IsDBNull("ActionTaken") ? null : reader.GetString("ActionTaken"),
                        Findings = reader.IsDBNull("Findings") ? null : reader.GetString("Findings"),
                        Agreement = reader.IsDBNull("Agreement") ? null : reader.GetString("Agreement"),
                        PenaltyAction = reader.IsDBNull("PenaltyAction") ? null : reader.GetString("PenaltyAction"),
                        Status = reader.GetString("Status"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        ParentContactType = reader.IsDBNull("ParentContactType") ? null : reader.GetString("ParentContactType"),
                        ParentContactName = reader.IsDBNull("ParentContactName") ? null : reader.GetString("ParentContactName"),
                        ParentMeetingDate = reader.IsDBNull("ParentMeetingDate") ? (DateTime?)null : reader.GetDateTime("ParentMeetingDate"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        VictimName = reader.IsDBNull("VictimName") ? null : reader.GetString("VictimName"),
                        ComplainantName = reader.IsDBNull("ComplainantName") ? null : reader.GetString("ComplainantName"),
                        ComplainantContact = reader.IsDBNull("ComplainantContact") ? null : reader.GetString("ComplainantContact"),
                        FathersName = reader.IsDBNull("FathersName") ? null : reader.GetString("FathersName"),
                        MothersName = reader.IsDBNull("MothersName") ? null : reader.GetString("MothersName"),
                        GuardianName = reader.IsDBNull("GuardianName") ? null : reader.GetString("GuardianName")
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting simplified case record by Escalation ID: {EscalationID}", escalationId);
            }
            return null;
        }

        public async Task<SimplifiedStudentProfileCaseRecordModel?> GetSimplifiedCaseRecordByIncidentIdAsync(int incidentId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT s.*, 
                           sir.SchoolName as SchoolName,
                           sir.Division as Division,
                           sir.VictimName as VictimName,
                           sir.FullName as ComplainantName,
                           sir.ComplainantContactNumber as ComplainantContact,
                           sch.Region as Region,
                           sch.District as District
                    FROM simplifiedstudentprofilecaserecords s
                    LEFT JOIN simplifiedincidentreports sir ON s.IncidentID = sir.IncidentID
                    LEFT JOIN schools sch ON sir.SchoolName = sch.SchoolName
                    WHERE s.IncidentID = @IncidentID
                    ORDER BY s.DateCreated DESC
                    LIMIT 1";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@IncidentID", incidentId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SimplifiedStudentProfileCaseRecordModel
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        EscalationID = reader.IsDBNull("EscalationID") ? (int?)null : reader.GetInt32("EscalationID"),
                        RespondentName = reader.GetString("RespondentName"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        ViolationCategory = reader.IsDBNull("ViolationCategory") ? null : reader.GetString("ViolationCategory"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        StudentSignatureBase64 = reader.IsDBNull("StudentSignatureBase64") ? null : reader.GetString("StudentSignatureBase64"),
                        DateOfBirth = reader.IsDBNull("DateOfBirth") ? (DateTime?)null : reader.GetDateTime("DateOfBirth"),
                        Age = reader.IsDBNull("Age") ? (int?)null : reader.GetInt32("Age"),
                        Sex = reader.IsDBNull("Sex") ? null : reader.GetString("Sex"),
                        Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel"),
                        TrackStrand = reader.IsDBNull("TrackStrand") ? null : reader.GetString("TrackStrand"),
                        Section = reader.IsDBNull("Section") ? null : reader.GetString("Section"),
                        AdviserName = reader.IsDBNull("AdviserName") ? null : reader.GetString("AdviserName"),
                        ActionTaken = reader.IsDBNull("ActionTaken") ? null : reader.GetString("ActionTaken"),
                        Findings = reader.IsDBNull("Findings") ? null : reader.GetString("Findings"),
                        Agreement = reader.IsDBNull("Agreement") ? null : reader.GetString("Agreement"),
                        PenaltyAction = reader.IsDBNull("PenaltyAction") ? null : reader.GetString("PenaltyAction"),
                        Status = reader.GetString("Status"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        ParentContactType = reader.IsDBNull("ParentContactType") ? null : reader.GetString("ParentContactType"),
                        ParentContactName = reader.IsDBNull("ParentContactName") ? null : reader.GetString("ParentContactName"),
                        ParentMeetingDate = reader.IsDBNull("ParentMeetingDate") ? (DateTime?)null : reader.GetDateTime("ParentMeetingDate"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        VictimName = reader.IsDBNull("VictimName") ? null : reader.GetString("VictimName"),
                        ComplainantName = reader.IsDBNull("ComplainantName") ? null : reader.GetString("ComplainantName"),
                        ComplainantContact = reader.IsDBNull("ComplainantContact") ? null : reader.GetString("ComplainantContact"),
                        FathersName = reader.IsDBNull("FathersName") ? null : reader.GetString("FathersName"),
                        MothersName = reader.IsDBNull("MothersName") ? null : reader.GetString("MothersName"),
                        GuardianName = reader.IsDBNull("GuardianName") ? null : reader.GetString("GuardianName")
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting simplified case record by Incident ID: {IncidentID}", incidentId);
            }
            return null;
        }

        public async Task<SimplifiedStudentProfileCaseRecordModel?> GetSimplifiedCaseRecordByIdAsync(int recordId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // JOIN with SimplifiedIncidentReports and schools to get school info
                var query = @"
                    SELECT s.*, 
                           sir.SchoolName as SchoolName,
                           sir.Division as Division,
                           sir.VictimName as VictimName,
                           sir.FullName as ComplainantName,
                           sir.ComplainantContactNumber as ComplainantContact,
                           sch.Region as Region,
                           sch.District as District
                    FROM simplifiedstudentprofilecaserecords s
                    LEFT JOIN simplifiedincidentreports sir ON s.IncidentID = sir.IncidentID
                    LEFT JOIN schools sch ON sir.SchoolName = sch.SchoolName
                    WHERE s.RecordID = @RecordID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@RecordID", recordId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SimplifiedStudentProfileCaseRecordModel
                    {
                        RecordID = reader.GetInt32("RecordID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? (int?)null : reader.GetInt32("IncidentID"),
                        EscalationID = reader.IsDBNull("EscalationID") ? (int?)null : reader.GetInt32("EscalationID"),
                        RespondentName = reader.GetString("RespondentName"),
                        DateOfOffense = reader.GetDateTime("DateOfOffense"),
                        ViolationCommitted = reader.GetString("ViolationCommitted"),
                        ViolationCategory = reader.IsDBNull("ViolationCategory") ? null : reader.GetString("ViolationCategory"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        StudentSignatureBase64 = reader.IsDBNull("StudentSignatureBase64") ? null : reader.GetString("StudentSignatureBase64"),
                        DateOfBirth = reader.IsDBNull("DateOfBirth") ? (DateTime?)null : reader.GetDateTime("DateOfBirth"),
                        Age = reader.IsDBNull("Age") ? (int?)null : reader.GetInt32("Age"),
                        Sex = reader.IsDBNull("Sex") ? null : reader.GetString("Sex"),
                        Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel"),
                        TrackStrand = reader.IsDBNull("TrackStrand") ? null : reader.GetString("TrackStrand"),
                        Section = reader.IsDBNull("Section") ? null : reader.GetString("Section"),
                        AdviserName = reader.IsDBNull("AdviserName") ? null : reader.GetString("AdviserName"),
                        ActionTaken = reader.IsDBNull("ActionTaken") ? null : reader.GetString("ActionTaken"),
                        Findings = reader.IsDBNull("Findings") ? null : reader.GetString("Findings"),
                        Agreement = reader.IsDBNull("Agreement") ? null : reader.GetString("Agreement"),
                        PenaltyAction = reader.IsDBNull("PenaltyAction") ? null : reader.GetString("PenaltyAction"),
                        Status = reader.GetString("Status"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        ParentContactType = reader.IsDBNull("ParentContactType") ? null : reader.GetString("ParentContactType"),
                        ParentContactName = reader.IsDBNull("ParentContactName") ? null : reader.GetString("ParentContactName"),
                        ParentMeetingDate = reader.IsDBNull("ParentMeetingDate") ? (DateTime?)null : reader.GetDateTime("ParentMeetingDate"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        VictimName = reader.IsDBNull("VictimName") ? null : reader.GetString("VictimName"),
                        ComplainantName = reader.IsDBNull("ComplainantName") ? null : reader.GetString("ComplainantName"),
                        ComplainantContact = reader.IsDBNull("ComplainantContact") ? null : reader.GetString("ComplainantContact"),
                        FathersName = reader.IsDBNull("FathersName") ? null : reader.GetString("FathersName"),
                        MothersName = reader.IsDBNull("MothersName") ? null : reader.GetString("MothersName"),
                        GuardianName = reader.IsDBNull("GuardianName") ? null : reader.GetString("GuardianName")
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting simplified case record by ID: {RecordID}", recordId);
            }
            return null;
        }
        public async Task<bool> SendSimplifiedCaseToTeacherAsync(int recordId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = "UPDATE simplifiedstudentprofilecaserecords SET Status = 'Pending Teacher' WHERE RecordID = @RecordID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@RecordID", recordId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending simplified case to teacher: {RecordID}", recordId);
                return false;
            }
        }

        public async Task<bool> UpdatePartBAsync(int recordId, string? actionTaken, string? findings, string? agreement, string? penaltyAction, string? status)
        {
            try
            {
                _logger.LogInformation("UpdatePartBAsync called for RecordID: {RecordID}, Status: {Status}", recordId, status ?? "NULL");

                var connectionString = _dbConnections.GetConnection();
                
                // First, get the IncidentID before updating
                int? incidentId = null;
                using (var connection1 = new MySqlConnection(connectionString))
                {
                    await connection1.OpenAsync();
                    var getIncidentQuery = "SELECT IncidentID FROM simplifiedstudentprofilecaserecords WHERE RecordID = @RecordID";
                    using var getCmd = new MySqlCommand(getIncidentQuery, connection1);
                    getCmd.Parameters.AddWithValue("@RecordID", recordId);
                    var result = await getCmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        incidentId = Convert.ToInt32(result);
                    }
                }

                // Update the Case Record with Part B fields
                using (var connection2 = new MySqlConnection(connectionString))
                {
                    await connection2.OpenAsync();
                    var query = @"
                        UPDATE simplifiedstudentprofilecaserecords
                        SET ActionTaken = COALESCE(@ActionTaken, ActionTaken),
                            Findings = COALESCE(@Findings, Findings),
                            Agreement = COALESCE(@Agreement, Agreement),
                            PenaltyAction = COALESCE(@PenaltyAction, PenaltyAction),
                            Status = COALESCE(@Status, Status)
                        WHERE RecordID = @RecordID";

                    using var command = new MySqlCommand(query, connection2);
                    command.Parameters.AddWithValue("@RecordID", recordId);
                    command.Parameters.AddWithValue("@ActionTaken", string.IsNullOrEmpty(actionTaken) ? (object)DBNull.Value : actionTaken);
                    command.Parameters.AddWithValue("@Findings", string.IsNullOrEmpty(findings) ? (object)DBNull.Value : findings);
                    command.Parameters.AddWithValue("@Agreement", string.IsNullOrEmpty(agreement) ? (object)DBNull.Value : agreement);
                    command.Parameters.AddWithValue("@PenaltyAction", string.IsNullOrEmpty(penaltyAction) ? (object)DBNull.Value : penaltyAction);
                    command.Parameters.AddWithValue("@Status", string.IsNullOrEmpty(status) ? (object)DBNull.Value : status);

                    var rows = await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("UpdatePartBAsync - SimplifiedStudentProfileCaseRecords updated, Rows affected: {Rows}", rows);
                }
                
                // Sync status to Incident Report if Status changed and we have an IncidentID
                if (!string.IsNullOrEmpty(status) && incidentId.HasValue)
                {
                    using (var connection3 = new MySqlConnection(connectionString))
                    {
                        await connection3.OpenAsync();
                        var syncQuery = "UPDATE simplifiedincidentreports SET Status = @Status WHERE IncidentID = @IncidentID";
                        using var syncCmd = new MySqlCommand(syncQuery, connection3);
                        syncCmd.Parameters.AddWithValue("@Status", status);
                        syncCmd.Parameters.AddWithValue("@IncidentID", incidentId.Value);
                        var syncRows = await syncCmd.ExecuteNonQueryAsync();
                        _logger.LogInformation("Synced status '{Status}' to SimplifiedIncidentReports {IncidentID}, Rows affected: {Rows}", status, incidentId.Value, syncRows);
                    }
                }

                _logger.LogInformation("UpdatePartBAsync completed for RecordID: {RecordID}", recordId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Part B for RecordID: {RecordID}", recordId);
                return false;
            }
        }
    }
}
