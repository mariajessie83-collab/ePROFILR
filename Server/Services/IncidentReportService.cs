using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using SharedProject;
using Microsoft.Extensions.Logging;
using Server.Data;

namespace Server.Services
{
    public class IncidentReportService
    {
        private readonly Dbconnections _dbConnections;
        private readonly ILogger<IncidentReportService> _logger;

        public IncidentReportService(Dbconnections dbConnections, ILogger<IncidentReportService> logger)
        {
            _dbConnections = dbConnections;
            _logger = logger;
        }

        public async Task<List<PODTeacher>> GetPODTeachersAsync()
        {
            var podTeachers = new List<PODTeacher>();

            try
            {
                _logger.LogInformation("Starting to get POD teachers from database");
                var connectionString = _dbConnections.GetConnection();
                _logger.LogInformation("Connection string: {ConnectionString}", connectionString);

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Database connection opened successfully");

                // First, let's check if there are any teachers at all
                var checkQuery = "SELECT COUNT(*) FROM teachers";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                var totalTeachers = await checkCommand.ExecuteScalarAsync();
                _logger.LogInformation("Total teachers in database: {TotalTeachers}", totalTeachers);

                // Check if there are any POD teachers
                var podCheckQuery = "SELECT COUNT(*) FROM teachers WHERE Position = 'POD'";
                using var podCheckCommand = new MySqlCommand(podCheckQuery, connection);
                var podCount = await podCheckCommand.ExecuteScalarAsync();
                _logger.LogInformation("POD teachers count: {PODCount}", podCount);

                var query = @"
                    SELECT t.TeacherID, t.TeacherName, t.Position, t.SchoolName, t.Division, t.Region, t.District
                    FROM teachers t 
                    WHERE t.Position = 'POD' AND t.IsActive = 1
                    ORDER BY t.TeacherName";

                _logger.LogInformation("Executing query: {Query}", query);

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var teacher = new PODTeacher
                    {
                        TeacherID = reader.GetInt32("TeacherID"),
                        TeacherName = reader.GetString("TeacherName"),
                        Position = reader.GetString("Position")
                    };
                    podTeachers.Add(teacher);
                    _logger.LogInformation("Found POD teacher: {TeacherName} (ID: {TeacherID})", teacher.TeacherName, teacher.TeacherID);
                }

                _logger.LogInformation("Total POD teachers found: {Count}", podTeachers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting POD teachers: {Message}", ex.Message);
                Console.WriteLine($"Error getting POD teachers: {ex.Message}");
            }

            return podTeachers;
        }

        public async Task<Studentclass?> GetStudentInfoByUsernameAsync(string username)
        {
            try
            {
                _logger.LogInformation("Getting student information for username: {Username}", username);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Try to get Strand or TrackStrand (whichever exists)
                var query = @"
                    SELECT st.StudentID, st.StudentName, st.GradeLevel, st.Section,
                           COALESCE(st.SchoolName, '') as SchoolName,
                           COALESCE(st.SchoolID, NULL) as SchoolID
                    FROM students st
                    INNER JOIN users u ON st.UserID = u.UserID
                    WHERE u.Username = @Username AND st.IsActive = 1
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var studentId = reader.GetInt32("StudentID");
                    var studentName = reader.GetString("StudentName");
                    var gradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel");
                    var section = reader.IsDBNull("Section") ? null : reader.GetString("Section");
                    var schoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName");
                    var schoolID = reader.IsDBNull("SchoolID") ? (int?)null : reader.GetInt32("SchoolID");
                    
                    reader.Close();
                    
                    // Get Strand separately (try both column names)
                    string? strand = null;
                    try
                    {
                        var strandQuery = "SELECT COALESCE(Strand, TrackStrand) as Strand FROM students WHERE StudentID = @StudentID";
                        using var strandCommand = new MySqlCommand(strandQuery, connection);
                        strandCommand.Parameters.AddWithValue("@StudentID", studentId);
                        var strandResult = await strandCommand.ExecuteScalarAsync();
                        strand = strandResult?.ToString();
                    }
                    catch
                    {
                        // If strand column doesn't exist, that's okay
                    }
                    
                    var student = new Studentclass
                    {
                        StudentID = studentId,
                        StudentName = studentName,
                        GradeLevel = gradeLevel,
                        Section = section,
                        Strand = strand,
                        SchoolName = schoolName,
                        School_ID = schoolID?.ToString()
                    };
                    
                    _logger.LogInformation("Found student: {StudentName} (ID: {StudentID})", student.StudentName, student.StudentID);
                    return student;
                }
                
                _logger.LogWarning("Student not found for username: {Username}", username);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student information for username {Username}: {Message}", username, ex.Message);
                return null;
            }
        }

        public async Task<(string SchoolName, string Division, string Region, string District)> GetPODLocationAsync(string username)
        {
            try
            {
                _logger.LogInformation("Getting location for user: {Username}", username);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // First, check if user is a teacher or student
                var userRoleQuery = "SELECT UserRole FROM users WHERE Username = @Username";
                using var userRoleCommand = new MySqlCommand(userRoleQuery, connection);
                userRoleCommand.Parameters.AddWithValue("@Username", username);
                
                var userRoleResult = await userRoleCommand.ExecuteScalarAsync();
                var userRole = userRoleResult?.ToString();
                _logger.LogInformation("User role for {Username}: {UserRole}", username, userRole);

                if (userRole == "student")
                {
                    // Get student's school information - check if SchoolName/SchoolID columns exist first
                    var studentQuery = @"
                        SELECT st.StudentName, st.Section, st.GradeLevel,
                               CASE 
                                   WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                                               WHERE TABLE_NAME = 'students' AND COLUMN_NAME = 'SchoolName')
                                   THEN st.SchoolName
                                   ELSE NULL
                               END as SchoolName,
                               CASE 
                                   WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                                               WHERE TABLE_NAME = 'students' AND COLUMN_NAME = 'SchoolID')
                                   THEN st.SchoolID
                                   ELSE NULL
                               END as SchoolID
                        FROM students st
                        INNER JOIN users u ON st.UserID = u.UserID
                        WHERE u.Username = @Username AND st.IsActive = 1";

                    using var studentCommand = new MySqlCommand(studentQuery, connection);
                    studentCommand.Parameters.AddWithValue("@Username", username);

                    using var studentReader = await studentCommand.ExecuteReaderAsync();
                    
                    if (await studentReader.ReadAsync())
                    {
                        var studentName = studentReader.GetString("StudentName");
                        var section = studentReader.IsDBNull("Section") ? "" : studentReader.GetString("Section");
                        var gradeLevel = studentReader.IsDBNull("GradeLevel") ? "" : studentReader.GetString("GradeLevel");
                        
                        // Check if SchoolName column exists and has data
                        var hasSchoolName = !studentReader.IsDBNull("SchoolName");
                        var schoolName = hasSchoolName ? studentReader.GetString("SchoolName") : "";
                        
                        // Check if SchoolID column exists and has data
                        var hasSchoolID = !studentReader.IsDBNull("SchoolID");
                        var schoolID = hasSchoolID ? studentReader.GetInt32("SchoolID") : 0;

                        _logger.LogInformation("Student {StudentName} - SchoolName exists: {HasSchoolName}, SchoolID exists: {HasSchoolID}, SchoolName: {SchoolName}, SchoolID: {SchoolID}", 
                            studentName, hasSchoolName, hasSchoolID, schoolName, schoolID);

                        // If we have SchoolID, try to get school details from schools table
                        if (hasSchoolID && schoolID > 0)
                        {
                            studentReader.Close();
                            studentCommand.Dispose();
                            
                            var schoolDetailsQuery = @"
                                SELECT SchoolName, Division, Region, District 
                                FROM schools 
                                WHERE SchoolID = @SchoolID AND IsActive = 1";
                            
                            using var schoolCommand = new MySqlCommand(schoolDetailsQuery, connection);
                            schoolCommand.Parameters.AddWithValue("@SchoolID", schoolID);
                            
                            using var schoolReader = await schoolCommand.ExecuteReaderAsync();
                            if (await schoolReader.ReadAsync())
                            {
                                var actualSchoolName = schoolReader.GetString("SchoolName");
                                var division = schoolReader.IsDBNull("Division") ? "" : schoolReader.GetString("Division");
                                var region = schoolReader.IsDBNull("Region") ? "" : schoolReader.GetString("Region");
                                var district = schoolReader.IsDBNull("District") ? "" : schoolReader.GetString("District");

                                _logger.LogInformation("Found student school details - School: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}", 
                                    actualSchoolName, division, region, district);

                                return (actualSchoolName, division, region, district);
                            }
                        }
                        
                        // If we have SchoolName directly in Students table
                        if (hasSchoolName && !string.IsNullOrEmpty(schoolName))
                        {
                            _logger.LogInformation("Using SchoolName from Students table: {SchoolName}", schoolName);
                            return (schoolName, "Unknown Division", "Unknown Region", "Unknown District");
                        }
                        
                        // Fallback: Use student's section/grade info to determine school
                        _logger.LogInformation("No school info found, using student details - Name: {StudentName}, Section: {Section}, Grade: {GradeLevel}", 
                            studentName, section, gradeLevel);
                        
                        // For now, return a dynamic school name based on student info
                        var dynamicSchoolName = $"{studentName}'s School - {gradeLevel} {section}";
                        return (dynamicSchoolName, "Student's Division", "Student's Region", "Student's District");
                    }
                }
                else if (userRole == "teacher")
                {
                    // Get teacher's school information (POD or any teacher)
                    var teacherQuery = @"
                        SELECT COALESCE(schools.SchoolName, t.SchoolName, 'Koronadal National Comprehensive High School') as SchoolName,
                               COALESCE(schools.Division, 'Koronadal City Division') as Division,
                               COALESCE(schools.Region, 'Region XII (SOCCSKSARGEN)') as Region,
                               COALESCE(schools.District, 'Koronadal City District') as District
                        FROM teachers t 
                        INNER JOIN users u ON t.UserID = u.UserID
                        LEFT JOIN schools ON t.SchoolID = schools.SchoolID
                        WHERE u.Username = @Username AND t.IsActive = 1";

                    using var teacherCommand = new MySqlCommand(teacherQuery, connection);
                    teacherCommand.Parameters.AddWithValue("@Username", username);

                    using var teacherReader = await teacherCommand.ExecuteReaderAsync();
                    
                    if (await teacherReader.ReadAsync())
                    {
                        var schoolName = teacherReader.IsDBNull("SchoolName") ? "Koronadal National Comprehensive High School" : teacherReader.GetString("SchoolName");
                        var division = teacherReader.IsDBNull("Division") ? "Koronadal City Division" : teacherReader.GetString("Division");
                        var region = teacherReader.IsDBNull("Region") ? "Region XII (SOCCSKSARGEN)" : teacherReader.GetString("Region");
                        var district = teacherReader.IsDBNull("District") ? "Koronadal City District" : teacherReader.GetString("District");

                        _logger.LogInformation("Found teacher location - School: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}", 
                            schoolName, division, region, district);

                        return (schoolName, division, region, district);
                    }
                }

                _logger.LogWarning("No location found for user: {Username}", username);
                return ("Koronadal National Comprehensive High School", "Koronadal City Division", "Region XII (SOCCSKSARGEN)", "Koronadal City District");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location for user {Username}: {Message}", username, ex.Message);
                return ("Koronadal National Comprehensive High School", "Koronadal City Division", "Region XII (SOCCSKSARGEN)", "Koronadal City District");
            }
        }

        public async Task<List<PODTeacher>> GetTeachersByLocationAsync(
            string? schoolName = null,
            string? division = null,
            string? region = null,
            string? district = null,
            string? position = null)
        {
            var teachers = new List<PODTeacher>();

            try
            {
                _logger.LogInformation("Getting teachers by location - School: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}, Position: {Position}", 
                    schoolName, division, region, district, position);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // First, let's check if we have any teachers at all
                var countQuery = "SELECT COUNT(*) FROM teachers WHERE IsActive = 1";
                using var countCommand = new MySqlCommand(countQuery, connection);
                var totalTeachers = await countCommand.ExecuteScalarAsync();
                _logger.LogInformation("Total active teachers in database: {TotalTeachers}", totalTeachers);

                // Join Teachers with schools table to get proper location filtering
                // Use COALESCE to handle cases where SchoolName might be in Teachers table or schools table
                var query = @"
                    SELECT t.TeacherID, t.TeacherName, t.Position, 
                           COALESCE(s.SchoolName, t.SchoolName, 'Koronadal National Comprehensive High School') as SchoolName
                    FROM teachers t 
                    LEFT JOIN schools s ON t.SchoolID = s.SchoolID
                    WHERE t.IsActive = 1";

                // Add location-based filtering - check both Teachers and schools tables
                if (!string.IsNullOrEmpty(schoolName))
                {
                    query += " AND (s.SchoolName = @SchoolName OR t.SchoolName = @SchoolName)";
                }

                if (!string.IsNullOrEmpty(division))
                {
                    query += " AND s.Division = @Division";
                }

                if (!string.IsNullOrEmpty(region))
                {
                    query += " AND s.Region = @Region";
                }

                if (!string.IsNullOrEmpty(district))
                {
                    query += " AND s.District = @District";
                }

                // Add position filter
                if (!string.IsNullOrEmpty(position))
                {
                    query += " AND t.Position = @Position";
                }

                query += " ORDER BY t.TeacherName";

                using var command = new MySqlCommand(query, connection);
                
                if (!string.IsNullOrEmpty(schoolName))
                {
                    command.Parameters.AddWithValue("@SchoolName", schoolName);
                }

                if (!string.IsNullOrEmpty(division))
                {
                    command.Parameters.AddWithValue("@Division", division);
                }

                if (!string.IsNullOrEmpty(region))
                {
                    command.Parameters.AddWithValue("@Region", region);
                }

                if (!string.IsNullOrEmpty(district))
                {
                    command.Parameters.AddWithValue("@District", district);
                }

                if (!string.IsNullOrEmpty(position))
                {
                    command.Parameters.AddWithValue("@Position", position);
                }

                using var reader = await command.ExecuteReaderAsync();
                
                _logger.LogInformation("=== DEBUGGING TEACHER QUERY ===");
                _logger.LogInformation("Executing query: {Query}", query);
                _logger.LogInformation("Query parameters - SchoolName: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}, Position: {Position}", 
                    schoolName, division, region, district, position);
                
                // Log each parameter value
                foreach (MySqlParameter param in command.Parameters)
                {
                    _logger.LogInformation("Parameter: {ParamName} = {ParamValue}", param.ParameterName, param.Value);
                }

                while (await reader.ReadAsync())
                {
                    var teacher = new PODTeacher
                    {
                        TeacherID = reader.GetInt32("TeacherID"),
                        TeacherName = reader.GetString("TeacherName"),
                        Position = reader.GetString("Position"),
                        GradeHandle = "" // Default empty since column doesn't exist
                    };
                    teachers.Add(teacher);
                    _logger.LogInformation("Found teacher: {TeacherName} (ID: {TeacherID}) Position: {Position}", 
                        teacher.TeacherName, teacher.TeacherID, teacher.Position);
                }

                _logger.LogInformation("Retrieved {Count} teachers filtered by location", teachers.Count);
                
                // DEBUG: If no teachers found, let's check if there are ANY active teachers at all
                if (teachers.Count == 0)
                {
                    _logger.LogWarning("No teachers found with location filters. Checking for ANY active teachers...");
                    
                    var debugQuery = "SELECT TeacherID, TeacherName, Position, SchoolName FROM teachers WHERE IsActive = 1";
                    using var debugCommand = new MySqlCommand(debugQuery, connection);
                    using var debugReader = await debugCommand.ExecuteReaderAsync();
                    
                    var allTeachers = new List<string>();
                    while (await debugReader.ReadAsync())
                    {
                        var teacherSchoolName = debugReader.IsDBNull("SchoolName") ? "NULL" : debugReader.GetString("SchoolName");
                        var teacherInfo = $"ID:{debugReader.GetInt32("TeacherID")}, Name:{debugReader.GetString("TeacherName")}, Position:{debugReader.GetString("Position")}, School:{teacherSchoolName}";
                        allTeachers.Add(teacherInfo);
                    }
                    
                    _logger.LogInformation("All active teachers in database: {Teachers}", string.Join("; ", allTeachers));
                    
                    // If we have teachers but no location filtering worked, try a simpler query without location filters
                    if (allTeachers.Count > 0 && !string.IsNullOrEmpty(position))
                    {
                        _logger.LogInformation("Trying fallback query without location filters...");
                        var fallbackQuery = "SELECT TeacherID, TeacherName, Position FROM teachers WHERE IsActive = 1 AND Position = @Position";
                        using var fallbackCommand = new MySqlCommand(fallbackQuery, connection);
                        fallbackCommand.Parameters.AddWithValue("@Position", position);
                        using var fallbackReader = await fallbackCommand.ExecuteReaderAsync();
                        
                        while (await fallbackReader.ReadAsync())
                        {
                            var teacher = new PODTeacher
                            {
                                TeacherID = fallbackReader.GetInt32("TeacherID"),
                                TeacherName = fallbackReader.GetString("TeacherName"),
                                Position = fallbackReader.GetString("Position"),
                                GradeHandle = ""
                            };
                            teachers.Add(teacher);
                            _logger.LogInformation("Added teacher via fallback: {TeacherName} (ID: {TeacherID})", teacher.TeacherName, teacher.TeacherID);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teachers by location: {Message}", ex.Message);
            }

            return teachers;
        }

        public async Task<List<Studentclass>> GetStudentsByLocationAsync(
            string? schoolName = null,
            string? division = null,
            string? region = null,
            string? district = null)
        {
            var students = new List<Studentclass>();

            try
            {
                _logger.LogInformation("Getting students by location - School: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}", 
                    schoolName, division, region, district);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Join Students with schools table to get proper location filtering
                var query = @"
                    SELECT s.StudentID, s.UserID, s.StudentName, s.Gender, s.Section, s.GradeLevel, s.Strand,
                           s.SchoolYear, s.ParentName, s.ParentContact, s.TeacherID, s.IsActive, s.DateRegister,
                           s.SchoolID, s.SchoolName, s.School_ID,
                           COALESCE(sc.SchoolName, s.SchoolName, 'Koronadal National Comprehensive High School') as ActualSchoolName,
                           COALESCE(sc.Division, 'Koronadal City Division') as Division,
                           COALESCE(sc.Region, 'Region XII (SOCCSKSARGEN)') as Region,
                           COALESCE(sc.District, 'Koronadal City District') as District
                    FROM students s 
                    LEFT JOIN schools sc ON s.SchoolID = sc.SchoolID
                    WHERE s.IsActive = 1";

                // Add location-based filtering
                if (!string.IsNullOrEmpty(schoolName))
                {
                    query += " AND (sc.SchoolName = @SchoolName OR s.SchoolName = @SchoolName)";
                }

                if (!string.IsNullOrEmpty(division))
                {
                    query += " AND sc.Division = @Division";
                }

                if (!string.IsNullOrEmpty(region))
                {
                    query += " AND sc.Region = @Region";
                }

                if (!string.IsNullOrEmpty(district))
                {
                    query += " AND sc.District = @District";
                }

                query += " ORDER BY s.StudentName";

                using var command = new MySqlCommand(query, connection);
                
                if (!string.IsNullOrEmpty(schoolName))
                {
                    command.Parameters.AddWithValue("@SchoolName", schoolName);
                }

                if (!string.IsNullOrEmpty(division))
                {
                    command.Parameters.AddWithValue("@Division", division);
                }

                if (!string.IsNullOrEmpty(region))
                {
                    command.Parameters.AddWithValue("@Region", region);
                }

                if (!string.IsNullOrEmpty(district))
                {
                    command.Parameters.AddWithValue("@District", district);
                }

                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var student = new Studentclass
                    {
                        StudentID = reader.GetInt32("StudentID"),
                        UserID = reader.GetInt32("UserID"),
                        StudentName = reader.GetString("StudentName"),
                        Gender = reader.IsDBNull("Gender") ? string.Empty : reader.GetString("Gender"),
                        Section = reader.IsDBNull("Section") ? null : reader.GetString("Section"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel"),
                        Strand = reader.IsDBNull("Strand") ? null : reader.GetString("Strand"),
                        SchoolYear = reader.IsDBNull("SchoolYear") ? null : reader.GetString("SchoolYear"),
                        ParentName = reader.IsDBNull("ParentName") ? null : reader.GetString("ParentName"),
                        ParentContact = reader.IsDBNull("ParentContact") ? null : reader.GetString("ParentContact"),
                        TeacherID = reader.IsDBNull("TeacherID") ? null : reader.GetInt32("TeacherID"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DateRegister = reader.GetDateTime("DateRegister"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        SchoolName = reader.IsDBNull("ActualSchoolName") ? null : reader.GetString("ActualSchoolName"),
                        School_ID = reader.IsDBNull("School_ID") ? null : reader.GetString("School_ID")
                    };
                    students.Add(student);
                    _logger.LogInformation("Found student: {StudentName} (ID: {StudentID}) Grade: {GradeLevel}, Section: {Section}", 
                        student.StudentName, student.StudentID, student.GradeLevel, student.Section);
                }

                _logger.LogInformation("Retrieved {Count} students filtered by location", students.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students by location: {Message}", ex.Message);
            }

            return students;
        }

        public async Task<string?> GetStudentAdviserAsync(string studentName, string? schoolName = null)
        {
            try
            {
                _logger.LogInformation("Getting adviser for student: {StudentName}, School: {SchoolName}", studentName, schoolName);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Find student first, then get their adviser
                var query = @"
                    SELECT s.StudentID, s.TeacherID, s.SchoolName, sc.SchoolName as ActualSchoolName,
                           s.GradeLevel, s.Section
                    FROM students s
                    LEFT JOIN schools sc ON s.SchoolID = sc.SchoolID
                    WHERE UPPER(TRIM(s.StudentName)) = UPPER(TRIM(@StudentName))
                    AND s.IsActive = 1";

                if (!string.IsNullOrEmpty(schoolName))
                {
                    query += " AND (sc.SchoolName = @SchoolName OR s.SchoolName = @SchoolName)";
                }

                query += " LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentName", studentName);
                
                if (!string.IsNullOrEmpty(schoolName))
                {
                    command.Parameters.AddWithValue("@SchoolName", schoolName);
                }

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var teacherID = reader.IsDBNull("TeacherID") ? (int?)null : reader.GetInt32("TeacherID");
                    var gradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel");
                    var section = reader.IsDBNull("Section") ? null : reader.GetString("Section");
                    
                    if (teacherID.HasValue && teacherID.Value > 0)
                    {
                        reader.Close();
                        
                        // Get teacher name
                        var teacherQuery = @"
                            SELECT TeacherName 
                            FROM teachers 
                            WHERE TeacherID = @TeacherID 
                            AND Position = 'Adviser' 
                            AND IsActive = 1 
                            LIMIT 1";
                        
                        using var teacherCommand = new MySqlCommand(teacherQuery, connection);
                        teacherCommand.Parameters.AddWithValue("@TeacherID", teacherID.Value);
                        
                        using var teacherReader = await teacherCommand.ExecuteReaderAsync();
                        if (await teacherReader.ReadAsync())
                        {
                            var adviserName = teacherReader.GetString("TeacherName");
                            _logger.LogInformation("Found adviser {AdviserName} for student {StudentName}", adviserName, studentName);
                            return adviserName;
                        }
                    }

                    // Fallback: try to match adviser by grade level and section if teacher ID is not set
                    if (!teacherID.HasValue || teacherID.Value <= 0)
                    {
                        reader.Close();

                        var fallbackQuery = @"
                            SELECT TeacherName 
                            FROM teachers 
                            WHERE IsActive = 1 
                              AND (Position = 'Adviser' OR Position = 'Class Adviser' OR Position = 'Class Adviser ')
                              AND (@GradeLevel IS NULL OR GradeLevel = @GradeLevel)
                              AND (@Section IS NULL OR Section = @Section)
                            ORDER BY TeacherName
                            LIMIT 1";

                        using var fallbackCommand = new MySqlCommand(fallbackQuery, connection);
                        fallbackCommand.Parameters.AddWithValue("@GradeLevel", (object?)gradeLevel ?? DBNull.Value);
                        fallbackCommand.Parameters.AddWithValue("@Section", (object?)section ?? DBNull.Value);

                        using var fallbackReader = await fallbackCommand.ExecuteReaderAsync();
                        if (await fallbackReader.ReadAsync())
                        {
                            var adviserName = fallbackReader.GetString("TeacherName");
                            _logger.LogInformation("Fallback adviser {AdviserName} matched by grade {GradeLevel} and section {Section} for student {StudentName}",
                                adviserName, gradeLevel, section, studentName);
                            return adviserName;
                        }
                    }
                }

                _logger.LogWarning("No adviser found for student: {StudentName}", studentName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student adviser: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<List<IncidentReportSummary>> GetIncidentReportsAsync(
            string? status = null, 
            int page = 1, 
            int pageSize = 10,
            string? schoolName = null,
            string? division = null,
            string? region = null,
            string? district = null,
            string? userLocation = null,
            DateTime? startDate = null)
        {
            var reports = new List<IncidentReportSummary>();

            try
            {
                _logger.LogInformation("Getting incident reports with status: {Status}, startDate: {StartDate}, location filters: School={School}, Division={Division}, Region={Region}, District={District}, UserLocation={UserLocation}", 
                    status, startDate, schoolName, division, region, district, userLocation);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // switch to SimplifiedIncidentReports table
                // IMPROVED JOIN: Use TRIM/UPPER on SchoolName to ensure matches even with casing/spacing differences
                var query = @"
                    SELECT sir.IncidentID, sir.FullName as ComplainantName, sir.VictimName, sir.RespondentName, sir.IncidentType, 
                           '' as OtherIncidentType, sir.Description as IncidentDescription, sir.DateReported, sir.Status, 
                           sir.ReferenceNumber as Reference_number, sir.SchoolName, sir.Division, 
                           COALESCE(s.Region, '') as Region, COALESCE(s.District, '') as District,
                           sir.EvidencePhotoBase64, 0 as IsSentToGuidance, NULL as DateSentToGuidance, '' as GuidanceCounselorName, '' as ReferredBy,
                           COALESCE(vt.ViolationCategory, 'Incident Report') as LevelOfOffense,
                           '' as GradeLevel
                    FROM SimplifiedIncidentReports sir
                    LEFT JOIN schools s ON TRIM(UPPER(sir.SchoolName)) = TRIM(UPPER(s.SchoolName))
                    LEFT JOIN ViolationTypes vt ON UPPER(TRIM(sir.IncidentType)) = UPPER(TRIM(vt.ViolationName)) AND (vt.IsActive = 1 OR vt.IsActive IS NULL)
                    WHERE (sir.IsActive = 1 OR sir.IsActive IS NULL)
                    ";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND sir.Status = @Status";
                }

                if (startDate.HasValue)
                {
                    query += " AND sir.DateReported >= @StartDate";
                }

                // Add location-based filtering
                if (!string.IsNullOrEmpty(schoolName))
                {
                    // Use TRIM and case-insensitive comparison for more robust matching
                    query += " AND TRIM(UPPER(sir.SchoolName)) = TRIM(UPPER(@SchoolName))";
                }
                else 
                {
                    // Only apply other filters if SchoolName is not specified
                    if (!string.IsNullOrEmpty(division))
                    {
                        // Use LIKE for fuzzy matching to handle "Division of South Cotabato" vs "South Cotabato"
                        // Check sir.Division directly AND joined table s.Division
                        query += " AND (TRIM(UPPER(sir.Division)) LIKE CONCAT('%', TRIM(UPPER(@Division)), '%') OR TRIM(UPPER(s.Division)) LIKE CONCAT('%', TRIM(UPPER(@Division)), '%'))";
                    }


                }

                query += " ORDER BY sir.DateReported DESC LIMIT @Offset, @PageSize";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                if (!string.IsNullOrEmpty(schoolName))
                {
                    command.Parameters.AddWithValue("@SchoolName", schoolName.Trim());
                }

                if (!string.IsNullOrEmpty(division))
                {
                    command.Parameters.AddWithValue("@Division", division.Trim());
                }

                if (startDate.HasValue)
                {
                    command.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
                }



                _logger.LogInformation("Executing query: {Query}", query);
                _logger.LogInformation("Query parameters - Offset: {Offset}, PageSize: {PageSize}, Status: {Status}, SchoolName: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}", 
                    (page - 1) * pageSize, pageSize, status, schoolName?.Trim(), division?.Trim(), region?.Trim(), district?.Trim());

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var report = new IncidentReportSummary
                    {
                        IncidentID = reader.GetInt32("IncidentID"),
                        ComplainantName = reader.IsDBNull("ComplainantName") ? string.Empty : reader.GetString("ComplainantName"),
                        VictimName = reader.IsDBNull("VictimName") ? string.Empty : reader.GetString("VictimName"),
                        RespondentName = reader.IsDBNull("RespondentName") ? string.Empty : reader.GetString("RespondentName"),
                        IncidentType = reader.IsDBNull("IncidentType") ? string.Empty : reader.GetString("IncidentType"),
                        OtherIncidentType = reader.IsDBNull("OtherIncidentType") ? null : reader.GetString("OtherIncidentType"),
                        IncidentDescription = reader.IsDBNull("IncidentDescription") ? string.Empty : reader.GetString("IncidentDescription"),
                        DateReported = reader.GetDateTime("DateReported"),
                        Status = reader.IsDBNull("Status") ? string.Empty : reader.GetString("Status"),
                        ReferenceNumber = reader.IsDBNull("Reference_number") ? null : reader.GetString("Reference_number"),
                        SchoolName = reader.IsDBNull("SchoolName") ? string.Empty : reader.GetString("SchoolName"),
                        Division = reader.IsDBNull("Division") ? string.Empty : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? string.Empty : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? string.Empty : reader.GetString("District"),
                        LevelOfOffense = reader.IsDBNull("LevelOfOffense") ? "Incident Report" : reader.GetString("LevelOfOffense"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? string.Empty : reader.GetString("GradeLevel"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        IsSentToGuidance = reader["IsSentToGuidance"] != DBNull.Value && Convert.ToBoolean(reader["IsSentToGuidance"]),
                        DateSentToGuidance = reader["DateSentToGuidance"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DateSentToGuidance"]) : null,
                        GuidanceCounselorName = reader.IsDBNull("GuidanceCounselorName") ? null : reader.GetString("GuidanceCounselorName"),
                        ReferredBy = reader.IsDBNull("ReferredBy") ? null : reader.GetString("ReferredBy")
                    };
                    reports.Add(report);
                    _logger.LogInformation("Added report: ID={ID}, Complainant={Complainant}, Victim={Victim}, Type={Type}, LevelOfOffense={LevelOfOffense}, Status={Status}, School={School}, Division={Division}", 
                        report.IncidentID, report.ComplainantName, report.VictimName, report.IncidentType, report.LevelOfOffense, report.Status, report.SchoolName, report.Division);
                }

                _logger.LogInformation("Retrieved {Count} incident reports", reports.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident reports: {Message}", ex.Message);
            }

            return reports;
        }

        public async Task<(int IncidentId, string ReferenceNumber)> CreateIncidentReportAsync(IncidentReportRequest request)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Generate Reference Number first (before insert to use in the INSERT)
                // We'll use a temporary ID for now, then update after insert
                var tempReferenceNumber = $"IR-{DateTime.UtcNow:yyyyMMdd}-TEMP";

                var query = @"
                    INSERT INTO incidentreports (
                        ComplainantName, ComplainantGrade, ComplainantStrand, ComplainantSection,
                        VictimName, RoomNumber, VictimContact, 
                        IncidentType, OtherIncidentType,
                        IncidentDescription,
                        RespondentName, AdviserName, PODIncharge, 
                        SchoolName, Division, Region, District,
                        EvidencePhotoPath, EvidencePhotoBase64,
                        Reference_number,
                        Status, CreatedBy, UpdatedBy
                    ) VALUES (
                        @ComplainantName, @ComplainantGrade, @ComplainantStrand, @ComplainantSection,
                        @VictimName, @RoomNumber, @VictimContact, 
                        @IncidentType, @OtherIncidentType,
                        @IncidentDescription,
                        @RespondentName, @AdviserName, @PODIncharge, 
                        @SchoolName, @Division, @Region, @District,
                        @EvidencePhotoPath, @EvidencePhotoBase64,
                        @ReferenceNumber,
                        @Status, @CreatedBy, @CreatedBy
                    )";

                _logger.LogInformation("Attempting to save incident report - School: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}", 
                    request.SchoolName, request.Division, request.Region, request.District);

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ComplainantName", request.ComplainantName);
                command.Parameters.AddWithValue("@ComplainantGrade", request.ComplainantGrade);
                command.Parameters.AddWithValue("@ComplainantStrand", request.ComplainantStrand ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ComplainantSection", request.ComplainantSection);
                command.Parameters.AddWithValue("@VictimName", request.VictimName);
                command.Parameters.AddWithValue("@RoomNumber", request.RoomNumber ?? "N/A");
                command.Parameters.AddWithValue("@VictimContact", request.VictimContact);
                command.Parameters.AddWithValue("@IncidentType", request.IncidentType);
                command.Parameters.AddWithValue("@OtherIncidentType", request.OtherIncidentType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IncidentDescription", request.IncidentDescription);
                command.Parameters.AddWithValue("@RespondentName", request.RespondentName);
                command.Parameters.AddWithValue("@AdviserName", request.AdviserName);
                command.Parameters.AddWithValue("@PODIncharge", request.PODIncharge ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SchoolName", request.SchoolName ?? "Unknown School");
                command.Parameters.AddWithValue("@Division", request.Division ?? "Unknown Division");
                command.Parameters.AddWithValue("@Region", request.Region ?? "Unknown Region");
                command.Parameters.AddWithValue("@District", request.District ?? "Unknown District");
                command.Parameters.AddWithValue("@EvidencePhotoPath", request.EvidencePhotoPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@EvidencePhotoBase64", request.EvidencePhotoBase64 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ReferenceNumber", tempReferenceNumber); 
                command.Parameters.AddWithValue("@Status", request.Status);
                command.Parameters.AddWithValue("@CreatedBy", 1); 

                await command.ExecuteNonQueryAsync();
                var newIncidentId = (int)command.LastInsertedId;

                // Generate actual Reference Number with the IncidentID
                var referenceNumber = $"IR-{DateTime.UtcNow:yyyyMMdd}-{newIncidentId.ToString().PadLeft(6, '0')}";

                // Update the Reference_number column with the actual reference number
                try
                {
                    var updateQuery = @"
                        UPDATE incidentreports 
                        SET Reference_number = @ReferenceNumber 
                        WHERE IncidentID = @IncidentID";
                    
                    using var updateCmd = new MySqlCommand(updateQuery, connection);
                    updateCmd.Parameters.AddWithValue("@ReferenceNumber", referenceNumber);
                    updateCmd.Parameters.AddWithValue("@IncidentID", newIncidentId);
                    
                    var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                    
                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation("Successfully saved reference number {ReferenceNumber} for IncidentID {IncidentID}", 
                            referenceNumber, newIncidentId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update reference number for IncidentID {IncidentID}", newIncidentId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update reference number for IncidentID {IncidentID}: {Error}", 
                        newIncidentId, ex.Message);
                    // Still return with generated reference number even if update failed
                }

                return (newIncidentId, referenceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating incident report: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<(int IncidentId, string ReferenceNumber)> CreateSimplifiedIncidentReportAsync(SimplifiedIncidentReportRequest request)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Generate Reference Number first (before insert to use in the INSERT)
                var tempReferenceNumber = $"IR-{DateTime.UtcNow:yyyyMMdd}-TEMP";

                var query = @"
                    INSERT INTO SimplifiedIncidentReports (
                        ViolationID, FullName, ComplainantContactNumber, RespondentName, AdviserName, VictimName, IncidentType,
                        Description, EvidencePhoto, EvidencePhotoBase64, ReferenceNumber,
                        Status, SchoolName, Division, DateReported, IsActive
                    ) VALUES (
                        @ViolationID, @FullName, @ComplainantContactNumber, @RespondentName, @AdviserName, @VictimName, @IncidentType,
                        @Description, @EvidencePhoto, @EvidencePhotoBase64, @ReferenceNumber,
                        @Status, @SchoolName, @Division, @DateReported, 1
                    )";

                _logger.LogInformation("Creating simplified incident report - School: {SchoolName}, Division: {Division}, Status: {Status}", 
                    request.SchoolName, request.Division, request.Status);

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ViolationID", DBNull.Value); // ViolationID can be set later if needed
                command.Parameters.AddWithValue("@FullName", request.FullName);
                command.Parameters.AddWithValue("@ComplainantContactNumber", request.ComplainantContactNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@RespondentName", request.RespondentName);
                command.Parameters.AddWithValue("@AdviserName", request.AdviserName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@VictimName", request.VictimName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IncidentType", request.IncidentType);
                command.Parameters.AddWithValue("@Description", request.Description);
                command.Parameters.AddWithValue("@EvidencePhoto", (object?)DBNull.Value);
                
                // Handle EvidencePhotoBase64 - ensure it's not empty string
                if (string.IsNullOrWhiteSpace(request.EvidencePhotoBase64))
                {
                    command.Parameters.AddWithValue("@EvidencePhotoBase64", DBNull.Value);
                }
                else
                {
                    // Log size for debugging
                    var photoSize = System.Text.Encoding.UTF8.GetByteCount(request.EvidencePhotoBase64);
                    _logger.LogInformation("Inserting photo with size: {PhotoSize} bytes ({PhotoSizeMB} MB)", 
                        photoSize, photoSize / 1024.0 / 1024.0);
                    command.Parameters.AddWithValue("@EvidencePhotoBase64", request.EvidencePhotoBase64);
                }
                
                command.Parameters.AddWithValue("@ReferenceNumber", tempReferenceNumber);
                command.Parameters.AddWithValue("@Status", request.Status ?? "Pending"); // Default to "Pending" if not provided
                command.Parameters.AddWithValue("@SchoolName", request.SchoolName);
                command.Parameters.AddWithValue("@Division", request.Division);
                command.Parameters.AddWithValue("@DateReported", DateTime.Now);

                await command.ExecuteNonQueryAsync();
                var newIncidentId = (int)command.LastInsertedId;

                // Generate actual Reference Number with the IncidentID
                var referenceNumber = $"IR-{DateTime.UtcNow:yyyyMMdd}-{newIncidentId.ToString().PadLeft(6, '0')}";

                // Update the ReferenceNumber column with the actual reference number
                try
                {
                    var updateQuery = @"
                        UPDATE simplifiedincidentreports 
                        SET ReferenceNumber = @ReferenceNumber 
                        WHERE IncidentID = @IncidentID";
                    
                    using var updateCmd = new MySqlCommand(updateQuery, connection);
                    updateCmd.Parameters.AddWithValue("@ReferenceNumber", referenceNumber);
                    updateCmd.Parameters.AddWithValue("@IncidentID", newIncidentId);
                    
                    var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                    
                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation("Successfully saved reference number {ReferenceNumber} for IncidentID {IncidentID}", 
                            referenceNumber, newIncidentId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update reference number for IncidentID {IncidentID}", newIncidentId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update reference number for IncidentID {IncidentID}: {Error}", 
                        newIncidentId, ex.Message);
                    // Still return with generated reference number even if update failed
                }

                // ==========================================================================================
                // AUTOMATIC ESCALATION TRIGGER
                // Check if this student now has 3 or more Minor offenses. If so, auto-escalate to POD.
                // ==========================================================================================
                try
                {
                    // 1. Check if the current incident (or previous ones) counts as Minor
                    // We check the TOTAL count of Minor offenses for this student
                    var countQuery = @"
                        SELECT COUNT(*) 
                        FROM simplifiedincidentreports s
                        JOIN violationtypes v ON UPPER(TRIM(s.IncidentType)) = UPPER(TRIM(v.ViolationName))
                        WHERE UPPER(TRIM(s.RespondentName)) = UPPER(TRIM(@RespondentName))
                        AND v.ViolationCategory = 'Minor'
                        AND (s.IsActive = 1 OR s.IsActive IS NULL)";

                    using var countCmd = new MySqlCommand(countQuery, connection);
                    countCmd.Parameters.AddWithValue("@RespondentName", request.RespondentName);
                    var minorCountObj = await countCmd.ExecuteScalarAsync();
                    var minorCount = minorCountObj != DBNull.Value ? Convert.ToInt32(minorCountObj) : 0;

                    if (minorCount >= 3)
                    {
                        // 2. Check if already escalated (Active)
                        var checkEscalationQuery = @"
                            SELECT COUNT(*) FROM caseescalations 
                            WHERE UPPER(TRIM(StudentName)) = UPPER(TRIM(@StudentName))
                            AND Status NOT IN ('Resolved', 'Closed', 'Withdrawn')
                            AND IsActive = 1";
                        
                        using var checkEscCmd = new MySqlCommand(checkEscalationQuery, connection);
                        checkEscCmd.Parameters.AddWithValue("@StudentName", request.RespondentName);
                        var existingEscalationsObj = await checkEscCmd.ExecuteScalarAsync();
                        var existingEscalations = existingEscalationsObj != DBNull.Value ? Convert.ToInt32(existingEscalationsObj) : 0;

                        if (existingEscalations == 0)
                        {
                            _logger.LogInformation("Automatic Escalation Triggered for {StudentName} (Count: {Count})", request.RespondentName, minorCount);

                            // 3. Get Student Details (Grade, Section, etc.) for the escalation record
                            string gradeLevel = "Unknown";
                            string section = "Unknown";
                            string trackStrand = "";
                            
                            var studentDetailsQuery = @"
                                SELECT GradeLevel, Section, TrackStrand, SchoolName 
                                FROM students 
                                WHERE UPPER(TRIM(StudentName)) = UPPER(TRIM(@StudentName)) 
                                LIMIT 1";

                            using var detailsCmd = new MySqlCommand(studentDetailsQuery, connection);
                            detailsCmd.Parameters.AddWithValue("@StudentName", request.RespondentName);
                            using var detailReader = await detailsCmd.ExecuteReaderAsync();
                            if (await detailReader.ReadAsync())
                            {
                                gradeLevel = detailReader.IsDBNull("GradeLevel") ? "Unknown" : detailReader.GetString("GradeLevel");
                                section = detailReader.IsDBNull("Section") ? "Unknown" : detailReader.GetString("Section");
                                trackStrand = detailReader.IsDBNull("TrackStrand") ? "" : detailReader.GetString("TrackStrand");
                            }
                            await detailReader.CloseAsync();

                            // 4. Get list of minor offenses for CaseDetails
                            var minorDetailsQuery = @"
                                SELECT IncidentType 
                                FROM simplifiedincidentreports s
                                JOIN violationtypes v ON UPPER(TRIM(s.IncidentType)) = UPPER(TRIM(v.ViolationName))
                                WHERE UPPER(TRIM(s.RespondentName)) = UPPER(TRIM(@RespondentName))
                                AND v.ViolationCategory = 'Minor'
                                AND (s.IsActive = 1 OR s.IsActive IS NULL)
                                ORDER BY s.DateReported DESC";
                            
                            var minorList = new List<string>();
                            using var minorListCmd = new MySqlCommand(minorDetailsQuery, connection);
                            minorListCmd.Parameters.AddWithValue("@RespondentName", request.RespondentName);
                            using var listReader = await minorListCmd.ExecuteReaderAsync();
                            while (await listReader.ReadAsync())
                            {
                                if (!listReader.IsDBNull("IncidentType"))
                                    minorList.Add(listReader.GetString("IncidentType"));
                            }
                            await listReader.CloseAsync();

                            string caseDetails = string.Join(", ", minorList.Distinct());

                            // 5. Insert Escalation
                            var insertEscQuery = @"
                                INSERT INTO caseescalations 
                                (StudentName, GradeLevel, Section, TrackStrand, SchoolName, MinorCaseCount, CaseDetails, ViolationCategory, EscalatedBy, EscalatedDate, Status, Notes, IsActive)
                                VALUES 
                                (@StudentName, @GradeLevel, @Section, @TrackStrand, @SchoolName, @MinorCaseCount, @CaseDetails, 'Major', @EscalatedBy, @EscalatedDate, 'Active', @Notes, 1)";

                            using var insertEscCmd = new MySqlCommand(insertEscQuery, connection);
                            insertEscCmd.Parameters.AddWithValue("@StudentName", request.RespondentName);
                            insertEscCmd.Parameters.AddWithValue("@GradeLevel", gradeLevel);
                            insertEscCmd.Parameters.AddWithValue("@Section", section);
                            insertEscCmd.Parameters.AddWithValue("@TrackStrand", trackStrand);
                            insertEscCmd.Parameters.AddWithValue("@SchoolName", request.SchoolName);
                            insertEscCmd.Parameters.AddWithValue("@MinorCaseCount", minorCount);
                            insertEscCmd.Parameters.AddWithValue("@CaseDetails", caseDetails);
                            insertEscCmd.Parameters.AddWithValue("@EscalatedBy", "System (Auto-Escalation)");
                            insertEscCmd.Parameters.AddWithValue("@EscalatedDate", DateTime.Now);
                            insertEscCmd.Parameters.AddWithValue("@Notes", "Automatically escalated due to accumulation of 3 or more minor offenses.");

                            await insertEscCmd.ExecuteNonQueryAsync();
                            _logger.LogInformation("Successfully auto-escalated {StudentName}", request.RespondentName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during automatic escalation check for {StudentName}", request.RespondentName);
                    // Don't throw, we don't want to fail the report creation just because auto-escalation failed
                }

                return (newIncidentId, referenceNumber);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL Error creating simplified incident report: ErrorCode={ErrorCode}, Number={Number}, Message={Message}", 
                    ex.ErrorCode, ex.Number, ex.Message);
                
                if (ex.Message.Contains("Packet for query is too large")) 
                {
                     _logger.LogError("CRITICAL: The image size exceeds the MySQL 'max_allowed_packet' setting. Please increase max_allowed_packet in my.ini/my.cnf.");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating simplified incident report: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<int> GetDailySubmissionCountAsync(string complainantName, DateTime date)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*) 
                    FROM incidentreports 
                    WHERE ComplainantName = @ComplainantName 
                    AND DATE(DateReported) = DATE(@Date)
                    AND IsActive = 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ComplainantName", complainantName);
                command.Parameters.AddWithValue("@Date", date);

                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                _logger.LogInformation("Daily submission count for {ComplainantName} on {Date}: {Count}", complainantName, date, count);
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily submission count: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<IncidentReportSummary>> GetIncidentReportsByComplainantAsync(string complainantName)
        {
            var reports = new List<IncidentReportSummary>();

            try
            {
                _logger.LogInformation("Getting incident reports for complainant: {ComplainantName}", complainantName);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ir.IncidentID, ir.ComplainantName, ir.VictimName, ir.RespondentName, ir.IncidentType, ir.OtherIncidentType, 
                           ir.IncidentDescription, COALESCE(ir.DateReported, ir.DateCreated) as DateReported, ir.Status, ir.Reference_number,
                           ir.SchoolName, ir.Division, ir.Region, ir.District, ir.EvidencePhotoBase64,
                           COALESCE(vt.ViolationCategory, 'Incident Report') as LevelOfOffense
                    FROM incidentreports ir
                    LEFT JOIN violationtypes vt ON (
                        UPPER(TRIM(vt.ViolationName)) = UPPER(TRIM(ir.IncidentType))
                        AND (vt.IsActive = 1 OR vt.IsActive IS NULL)
                    )
                    WHERE ir.ComplainantName = @ComplainantName AND ir.IsActive = 1
                    ORDER BY COALESCE(ir.DateReported, ir.DateCreated) DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ComplainantName", complainantName);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    reports.Add(new IncidentReportSummary
                    {
                        IncidentID = reader.GetInt32("IncidentID"),
                        ComplainantName = reader.IsDBNull("ComplainantName") ? string.Empty : reader.GetString("ComplainantName"),
                        VictimName = reader.IsDBNull("VictimName") ? string.Empty : reader.GetString("VictimName"),
                        RespondentName = reader.IsDBNull("RespondentName") ? string.Empty : reader.GetString("RespondentName"),
                        IncidentType = reader.IsDBNull("IncidentType") ? string.Empty : reader.GetString("IncidentType"),
                        OtherIncidentType = reader.IsDBNull("OtherIncidentType") ? null : reader.GetString("OtherIncidentType"),
                        IncidentDescription = reader.IsDBNull("IncidentDescription") ? string.Empty : reader.GetString("IncidentDescription"),
                        DateReported = reader.GetDateTime("DateReported"),
                        Status = reader.IsDBNull("Status") ? string.Empty : reader.GetString("Status"),
                        ReferenceNumber = reader.IsDBNull("Reference_number") ? null : reader.GetString("Reference_number"),
                        SchoolName = reader.IsDBNull("SchoolName") ? string.Empty : reader.GetString("SchoolName"),
                        Division = reader.IsDBNull("Division") ? string.Empty : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? string.Empty : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? string.Empty : reader.GetString("District"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        LevelOfOffense = reader.IsDBNull("LevelOfOffense") ? "Incident Report" : reader.GetString("LevelOfOffense")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident reports for complainant {ComplainantName}", complainantName);
                throw;
            }

            return reports;
        }

        public async Task<List<IncidentReportSummary>> GetIncidentReportsByRespondentAsync(string respondentName)
        {
            var reports = new List<IncidentReportSummary>();

            try
            {
                _logger.LogInformation("Getting incident reports for respondent: {RespondentName}", respondentName);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Use LIKE pattern matching to handle comma-separated respondent names
                var query = @"
                    SELECT ir.IncidentID, ir.ComplainantName, ir.VictimName, ir.RespondentName, ir.IncidentType, ir.OtherIncidentType, 
                           ir.IncidentDescription, COALESCE(ir.DateReported, ir.DateCreated) as DateReported, ir.Status, ir.Reference_number,
                           ir.SchoolName, ir.Division, ir.Region, ir.District, ir.EvidencePhotoBase64,
                           COALESCE(vt.ViolationCategory, 'Incident Report') as LevelOfOffense
                    FROM incidentreports ir
                    LEFT JOIN violationtypes vt ON (
                        UPPER(TRIM(vt.ViolationName)) = UPPER(TRIM(ir.IncidentType))
                        AND (vt.IsActive = 1 OR vt.IsActive IS NULL)
                    )
                    WHERE (
                        UPPER(TRIM(ir.RespondentName)) = UPPER(TRIM(@RespondentName))
                        OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT(UPPER(TRIM(@RespondentName)), ', %')
                        OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT('%, ', UPPER(TRIM(@RespondentName)), ', %')
                        OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT('%, ', UPPER(TRIM(@RespondentName)), '%')
                        OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT(UPPER(TRIM(@RespondentName)), ',%')
                        OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT('%,', UPPER(TRIM(@RespondentName)), ',%')
                        OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT('%,', UPPER(TRIM(@RespondentName)), '%')
                    )
                    AND ir.IsActive = 1
                    ORDER BY COALESCE(ir.DateReported, ir.DateCreated) DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@RespondentName", respondentName);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    reports.Add(new IncidentReportSummary
                    {
                        IncidentID = reader.GetInt32("IncidentID"),
                        ComplainantName = reader.IsDBNull("ComplainantName") ? string.Empty : reader.GetString("ComplainantName"),
                        VictimName = reader.IsDBNull("VictimName") ? string.Empty : reader.GetString("VictimName"),
                        RespondentName = reader.IsDBNull("RespondentName") ? string.Empty : reader.GetString("RespondentName"),
                        IncidentType = reader.IsDBNull("IncidentType") ? string.Empty : reader.GetString("IncidentType"),
                        OtherIncidentType = reader.IsDBNull("OtherIncidentType") ? null : reader.GetString("OtherIncidentType"),
                        IncidentDescription = reader.IsDBNull("IncidentDescription") ? string.Empty : reader.GetString("IncidentDescription"),
                        DateReported = reader.GetDateTime("DateReported"),
                        Status = reader.IsDBNull("Status") ? string.Empty : reader.GetString("Status"),
                        ReferenceNumber = reader.IsDBNull("Reference_number") ? null : reader.GetString("Reference_number"),
                        SchoolName = reader.IsDBNull("SchoolName") ? string.Empty : reader.GetString("SchoolName"),
                        Division = reader.IsDBNull("Division") ? string.Empty : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? string.Empty : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? string.Empty : reader.GetString("District"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        LevelOfOffense = reader.IsDBNull("LevelOfOffense") ? "Incident Report" : reader.GetString("LevelOfOffense")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident reports for respondent {RespondentName}", respondentName);
                throw;
            }

            return reports;
        }

        public async Task<List<IncidentReportSummary>> GetIncidentReportsByTeacherAsync(int teacherId, string? status = null, int page = 1, int pageSize = 10)
        {
            var reports = new List<IncidentReportSummary>();

            try
            {
                _logger.LogInformation("Getting incident reports for teacher ID: {TeacherId}, status: {Status}", teacherId, status);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Query to get incident reports where the student is either the Respondent (nireport) or Victim (nireport)
                // Only include if student is Respondent or Victim, NOT if they are just the Complainant
                // Join with ViolationTypes to get the LevelOfOffense (ViolationCategory)
                // Updated to handle comma-separated respondent names using LIKE pattern matching
                var query = @"
                    SELECT DISTINCT ir.IncidentID, ir.ComplainantName, ir.VictimName, ir.RespondentName, 
                           ir.IncidentType, ir.OtherIncidentType, ir.IncidentDescription,
                           COALESCE(ir.DateReported, ir.DateCreated) as DateReported, ir.Status, 
                           ir.Reference_number, ir.SchoolName, ir.Division, ir.Region, ir.District,
                           ir.EvidencePhotoBase64, COALESCE(vt.ViolationCategory, 'Incident Report') as LevelOfOffense
                    FROM incidentreports ir
                    INNER JOIN students s ON (
                        -- Check if student name matches any respondent in comma-separated list
                        -- Handles both single respondent and multiple comma-separated respondents
                        (UPPER(TRIM(ir.RespondentName)) = UPPER(TRIM(s.StudentName))
                         OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT(UPPER(TRIM(s.StudentName)), ', %')
                         OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT('%, ', UPPER(TRIM(s.StudentName)), ', %')
                         OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT('%, ', UPPER(TRIM(s.StudentName)), '%')
                         OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT(UPPER(TRIM(s.StudentName)), ',%')
                         OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT('%,', UPPER(TRIM(s.StudentName)), ',%')
                         OR UPPER(TRIM(ir.RespondentName)) LIKE CONCAT('%,', UPPER(TRIM(s.StudentName)), '%'))
                        OR
                        -- Check if student name matches victim
                        (UPPER(TRIM(ir.VictimName)) = UPPER(TRIM(s.StudentName)))
                    )
                    LEFT JOIN violationtypes vt ON (
                        UPPER(TRIM(vt.ViolationName)) = UPPER(TRIM(ir.IncidentType))
                        AND (vt.IsActive = 1 OR vt.IsActive IS NULL)
                    )
                    WHERE ir.IsActive = 1
                        AND s.TeacherID = @TeacherID
                        AND s.IsActive = 1";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND ir.Status = @Status";
                }

                query += " ORDER BY COALESCE(ir.DateReported, ir.DateCreated) DESC LIMIT @Offset, @PageSize";

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

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var report = new IncidentReportSummary
                    {
                        IncidentID = reader.GetInt32("IncidentID"),
                        ComplainantName = reader.IsDBNull("ComplainantName") ? string.Empty : reader.GetString("ComplainantName"),
                        VictimName = reader.IsDBNull("VictimName") ? string.Empty : reader.GetString("VictimName"),
                        RespondentName = reader.IsDBNull("RespondentName") ? string.Empty : reader.GetString("RespondentName"),
                        IncidentType = reader.IsDBNull("IncidentType") ? string.Empty : reader.GetString("IncidentType"),
                        OtherIncidentType = reader.IsDBNull("OtherIncidentType") ? null : reader.GetString("OtherIncidentType"),
                        IncidentDescription = reader.IsDBNull("IncidentDescription") ? string.Empty : reader.GetString("IncidentDescription"),
                        DateReported = reader.GetDateTime("DateReported"),
                        Status = reader.IsDBNull("Status") ? string.Empty : reader.GetString("Status"),
                        ReferenceNumber = reader.IsDBNull("Reference_number") ? null : reader.GetString("Reference_number"),
                        SchoolName = reader.IsDBNull("SchoolName") ? string.Empty : reader.GetString("SchoolName"),
                        Division = reader.IsDBNull("Division") ? string.Empty : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? string.Empty : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? string.Empty : reader.GetString("District"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? null : reader.GetString("EvidencePhotoBase64"),
                        LevelOfOffense = reader.IsDBNull("LevelOfOffense") ? "Incident Report" : reader.GetString("LevelOfOffense")
                    };
                    reports.Add(report);
                }

                _logger.LogInformation("Retrieved {Count} incident reports for teacher {TeacherID}", reports.Count, teacherId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident reports for teacher {TeacherID}: {Message}\nStack trace: {StackTrace}", teacherId, ex.Message, ex.StackTrace);
                throw;
            }

            return reports;
        }

        public async Task<IncidentReportModel?> GetIncidentReportByIdAsync(int incidentId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // First try SimplifiedIncidentReports
                var querySimplified = @"
                    SELECT 
                        sir.FullName as ComplainantName, '' as ComplainantGrade, '' as ComplainantStrand, '' as ComplainantSection,
                        sir.VictimName, '' as RoomNumber, '' as VictimContact, 
                        sir.IncidentType, '' as OtherIncidentType, sir.Description as IncidentDescription,
                        sir.RespondentName, sir.AdviserName, '' as PODIncharge, 
                        sir.SchoolName, sir.Division, COALESCE(s.Region, 'Unknown') as Region, COALESCE(s.District, 'Unknown') as District,
                        '' as EvidencePhotoPath, sir.EvidencePhotoBase64,
                        sir.DateReported, sir.Status, sir.CreatedBy, sir.UpdatedBy
                    FROM simplifiedincidentreports sir
                    LEFT JOIN schools s ON TRIM(UPPER(sir.SchoolName)) = TRIM(UPPER(s.SchoolName))
                    WHERE sir.IncidentID = @IncidentID AND (sir.IsActive = 1 OR sir.IsActive IS NULL)";

                using var commandSimplified = new MySqlCommand(querySimplified, connection);
                commandSimplified.Parameters.AddWithValue("@IncidentID", incidentId);

                using var readerSimplified = await commandSimplified.ExecuteReaderAsync();
                if (await readerSimplified.ReadAsync())
                {
                    return new IncidentReportModel
                    {
                        ComplainantName = readerSimplified.GetString("ComplainantName"),
                        ComplainantGrade = readerSimplified.GetString("ComplainantGrade"),
                        ComplainantStrand = readerSimplified.IsDBNull("ComplainantStrand") ? null : readerSimplified.GetString("ComplainantStrand"),
                        ComplainantSection = readerSimplified.GetString("ComplainantSection"),
                        VictimName = readerSimplified.IsDBNull("VictimName") ? "N/A" : readerSimplified.GetString("VictimName"),
                        RoomNumber = readerSimplified.IsDBNull("RoomNumber") ? null : readerSimplified.GetString("RoomNumber"),
                        VictimContact = readerSimplified.IsDBNull("VictimContact") ? "N/A" : readerSimplified.GetString("VictimContact"),
                        IncidentType = readerSimplified.IsDBNull("IncidentType") ? string.Empty : readerSimplified.GetString("IncidentType"),
                        OtherIncidentType = readerSimplified.IsDBNull("OtherIncidentType") ? null : readerSimplified.GetString("OtherIncidentType"),
                        IncidentDescription = readerSimplified.GetString("IncidentDescription"),
                        RespondentName = readerSimplified.GetString("RespondentName"),
                        AdviserName = readerSimplified.GetString("AdviserName"),
                        PODIncharge = readerSimplified.IsDBNull("PODIncharge") ? null : readerSimplified.GetString("PODIncharge"),
                        EvidencePhotoPath = readerSimplified.IsDBNull("EvidencePhotoPath") ? null : readerSimplified.GetString("EvidencePhotoPath"),
                        EvidencePhotoBase64 = readerSimplified.IsDBNull("EvidencePhotoBase64") ? null : readerSimplified.GetString("EvidencePhotoBase64"),
                        DateReported = readerSimplified.GetDateTime("DateReported"),
                        Status = readerSimplified.GetString("Status")
                    };
                }
                readerSimplified.Close();

                // If not found, fallback to legacy incidentreports
                _logger.LogInformation("Not found in SimplifiedIncidentReports, falling back to legacy incidentreports for ID: {IncidentID}", incidentId);

                var queryLegacy = @"
                    SELECT 
                        ComplainantName, ComplainantGrade, ComplainantStrand, ComplainantSection,
                        VictimName, RoomNumber, VictimContact, 
                        IncidentType, OtherIncidentType, IncidentDescription,
                        RespondentName, AdviserName, PODIncharge, 
                        SchoolName, Division, Region, District,
                        EvidencePhotoPath, EvidencePhotoBase64,
                        DateReported, Status, CreatedBy, UpdatedBy
                    FROM incidentreports 
                    WHERE IncidentID = @IncidentID AND IsActive = 1";

                using var commandLegacy = new MySqlCommand(queryLegacy, connection);
                commandLegacy.Parameters.AddWithValue("@IncidentID", incidentId);

                using var readerLegacy = await commandLegacy.ExecuteReaderAsync();
                if (await readerLegacy.ReadAsync())
                {
                    return new IncidentReportModel
                    {
                        ComplainantName = readerLegacy.GetString("ComplainantName"),
                        ComplainantGrade = readerLegacy.GetString("ComplainantGrade"),
                        ComplainantStrand = readerLegacy.IsDBNull("ComplainantStrand") ? null : readerLegacy.GetString("ComplainantStrand"),
                        ComplainantSection = readerLegacy.GetString("ComplainantSection"),
                        VictimName = readerLegacy.GetString("VictimName"),
                        RoomNumber = readerLegacy.IsDBNull("RoomNumber") ? null : readerLegacy.GetString("RoomNumber"),
                        VictimContact = readerLegacy.GetString("VictimContact"),
                        IncidentType = readerLegacy.IsDBNull("IncidentType") ? string.Empty : readerLegacy.GetString("IncidentType"),
                        OtherIncidentType = readerLegacy.IsDBNull("OtherIncidentType") ? null : readerLegacy.GetString("OtherIncidentType"),
                        IncidentDescription = readerLegacy.GetString("IncidentDescription"),
                        RespondentName = readerLegacy.GetString("RespondentName"),
                        AdviserName = readerLegacy.GetString("AdviserName"),
                        PODIncharge = readerLegacy.IsDBNull("PODIncharge") ? null : readerLegacy.GetString("PODIncharge"),
                        EvidencePhotoPath = readerLegacy.IsDBNull("EvidencePhotoPath") ? null : readerLegacy.GetString("EvidencePhotoPath"),
                        EvidencePhotoBase64 = readerLegacy.IsDBNull("EvidencePhotoBase64") ? null : readerLegacy.GetString("EvidencePhotoBase64"),
                        DateReported = readerLegacy.GetDateTime("DateReported"),
                        Status = readerLegacy.GetString("Status")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident report by ID: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> UpdateIncidentReportStatusAsync(int incidentId, string status, string? updatedBy = null)
        {
            try
            {
                _logger.LogInformation("Updating incident report {IncidentID} status to {Status}", incidentId, status);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // First try SimplifiedIncidentReports
                var querySimplified = @"
                    UPDATE simplifiedincidentreports 
                    SET Status = @Status
                    WHERE IncidentID = @IncidentID 
                    AND (IsActive = 1 OR IsActive IS NULL)";

                using var commandSimplified = new MySqlCommand(querySimplified, connection);
                commandSimplified.Parameters.AddWithValue("@Status", status);
                commandSimplified.Parameters.AddWithValue("@IncidentID", incidentId);

                var rowsAffected = await commandSimplified.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Successfully updated SimplifiedIncidentReport {IncidentID} status to {Status}. Syncing to case records...", incidentId, status);
                    
                    // Sync to SimplifiedStudentProfileCaseRecords
                    var updateCaseRecordsQuery = "UPDATE simplifiedstudentprofilecaserecords SET Status = @Status WHERE IncidentID = @IncidentID";
                    using var updateCaseCmd = new MySqlCommand(updateCaseRecordsQuery, connection);
                    updateCaseCmd.Parameters.AddWithValue("@Status", status);
                    updateCaseCmd.Parameters.AddWithValue("@IncidentID", incidentId);
                    var caseRows = await updateCaseCmd.ExecuteNonQueryAsync();
                    if (caseRows > 0) _logger.LogInformation("Synced Status '{Status}' to {Count} Case Records for IncidentID {IncidentID}", status, caseRows, incidentId);
                    
                    return true;
                }

                // If not found, fallback to legacy incidentreports
                _logger.LogInformation("Not found in SimplifiedIncidentReports, falling back to legacy incidentreports for ID: {IncidentID}", incidentId);
                
                var queryLegacy = @"
                    UPDATE incidentreports 
                    SET Status = @Status
                    WHERE IncidentID = @IncidentID 
                    AND IsActive = 1";

                using var commandLegacy = new MySqlCommand(queryLegacy, connection);
                commandLegacy.Parameters.AddWithValue("@Status", status);
                commandLegacy.Parameters.AddWithValue("@IncidentID", incidentId);

                rowsAffected = await commandLegacy.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Successfully updated legacy incidentreports {IncidentID} status to {Status}", incidentId, status);
                    return true;
                }

                _logger.LogWarning("No rows affected when updating incident report {IncidentID} status in either table.", incidentId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating incident report {IncidentID} status: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<List<CategoryTrendData>> GetCategoryTrendDataAsync(
            string? schoolName = null,
            string? division = null,
            string? region = null,
            string? district = null)
        {
            var trendData = new List<CategoryTrendData>();

            try
            {
                 _logger.LogInformation("=== GETTING CATEGORY TREND DATA ===");
                _logger.LogInformation("Parameters - School: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}", 
                    schoolName ?? "NULL", division ?? "NULL", region ?? "NULL", district ?? "NULL");
                
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Database connection opened successfully");

                // Step 1: Check if StudentProfileCaseRecords table has any data
                var checkQuery = "SELECT COUNT(*) FROM studentprofilecaserecords WHERE (IsActive = 1 OR IsActive IS NULL)";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                var totalRecordsObj = await checkCommand.ExecuteScalarAsync();
                var totalRecords = totalRecordsObj != null ? Convert.ToInt32(totalRecordsObj) : 0;
                _logger.LogInformation("Total active records in StudentProfileCaseRecords: {TotalRecords}", totalRecords);

                if (totalRecords == 0)
                {
                    _logger.LogWarning("WARNING: No active records found. Returning empty trend data.");
                    // Still return 12 months with zeros
                }

                // Step 2: Check what LevelOfOffense values exist (without location filters)
                var levelQuery = @"SELECT DISTINCT UPPER(TRIM(LevelOfOffense)) as Level, COUNT(*) as Count 
                                  FROM studentprofilecaserecords 
                                  WHERE (IsActive = 1 OR IsActive IS NULL) 
                                  AND LevelOfOffense IS NOT NULL 
                                  AND TRIM(LevelOfOffense) != ''
                                  GROUP BY UPPER(TRIM(LevelOfOffense))";
                var levels = new List<string>();
                using (var levelCommand = new MySqlCommand(levelQuery, connection))
                {
                    using (var levelReader = await levelCommand.ExecuteReaderAsync())
                    {
                        while (await levelReader.ReadAsync())
                        {
                            if (!levelReader.IsDBNull("Level"))
                            {
                                var level = levelReader.GetString("Level");
                                var count = levelReader.GetInt32("Count");
                                levels.Add($"{level} ({count})");
                            }
                        }
                    } // levelReader is now closed
                } // levelCommand is now disposed
                _logger.LogInformation("Distinct LevelOfOffense values: {Levels}", 
                    levels.Any() ? string.Join(", ", levels) : "NONE");

                // Step 3: Get current year months (Jan-Dec)
                var now = DateTime.Now;
                var currentYear = now.Year;
                var last12Months = Enumerable.Range(1, 12).Select(month => new DateTime(currentYear, month, 1)).ToList();
                _logger.LogInformation("Processing current year {Year}: {Months}", 
                    currentYear, string.Join(", ", last12Months.Select(m => m.ToString("MMM"))));

                // Step 4: Process each month
                foreach (var month in last12Months)
                {
                    var monthLabel = month.ToString("MMMM");
                    _logger.LogInformation("Processing month: {Month} ({Year})", monthLabel, month.Year);
                    
                    var minorCount = 0;
                    var majorCount = 0;
                    var prohibitedCount = 0;

                    try
                    {
                        // Try query with proper source (SimplifiedIncidentReports)
                        string query;
                        var hasLocationFilters = !string.IsNullOrEmpty(schoolName) || !string.IsNullOrEmpty(division) || 
                                                !string.IsNullOrEmpty(region) || !string.IsNullOrEmpty(district);

                        if (!hasLocationFilters)
                        {
                            // Query SimplifiedIncidentReports directly
                            query = @"
                                SELECT 
                                    COALESCE(vt.ViolationCategory, 'Uncategorized') as Level,
                                    COUNT(*) as Count
                                FROM simplifiedincidentreports sir
                                LEFT JOIN violationtypes vt ON UPPER(TRIM(sir.IncidentType)) = UPPER(TRIM(vt.ViolationName)) AND (vt.IsActive = 1 OR vt.IsActive IS NULL)
                                WHERE MONTH(sir.DateReported) = @Month 
                                AND YEAR(sir.DateReported) = @Year
                                AND (sir.IsActive = 1 OR sir.IsActive IS NULL)
                                AND sir.Status != 'Pending' 
                                AND sir.Status != 'Rejected'
                                GROUP BY COALESCE(vt.ViolationCategory, 'Uncategorized')";
                        }
                        else
                        {
                            // Query with location filters (using schools join for robust location matching if needed, 
                            // or filtering on SIR columns directly if they exist and are reliable. 
                            // Using SIR columns + School join for District/Region/Division safety)
                            query = @"
                                SELECT 
                                    COALESCE(vt.ViolationCategory, 'Uncategorized') as Level,
                                    COUNT(*) as Count
                                FROM simplifiedincidentreports sir
                                LEFT JOIN schools s ON TRIM(UPPER(sir.SchoolName)) = TRIM(UPPER(s.SchoolName))
                                LEFT JOIN violationtypes vt ON UPPER(TRIM(sir.IncidentType)) = UPPER(TRIM(vt.ViolationName)) AND (vt.IsActive = 1 OR vt.IsActive IS NULL)
                                WHERE MONTH(sir.DateReported) = @Month 
                                AND YEAR(sir.DateReported) = @Year
                                AND (sir.IsActive = 1 OR sir.IsActive IS NULL)
                                AND sir.Status != 'Pending' 
                                AND sir.Status != 'Rejected'";
                                
                            if (!string.IsNullOrEmpty(schoolName))
                            {
                                query += " AND TRIM(UPPER(sir.SchoolName)) = @SchoolName";
                            }
                            if (!string.IsNullOrEmpty(division))
                            {
                                query += " AND (TRIM(UPPER(s.Division)) = TRIM(UPPER(@Division)) OR TRIM(UPPER(sir.Division)) = TRIM(UPPER(@Division)))";
                            }
                            if (!string.IsNullOrEmpty(region))
                            {
                                query += " AND (TRIM(UPPER(s.Region)) = @Region OR TRIM(UPPER(sir.Region)) = @Region)";
                            }
                            if (!string.IsNullOrEmpty(district))
                            {
                                query += " AND TRIM(UPPER(s.District)) = TRIM(UPPER(@District))";
                            }
                            
                            query += " GROUP BY COALESCE(vt.ViolationCategory, 'Uncategorized')";
                        }

                        _logger.LogInformation("Executing query for {Month}", monthLabel);

                        using var command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@Month", month.Month);
                        command.Parameters.AddWithValue("@Year", month.Year);

                        // Add parameters only if they are used in the query
                        if (hasLocationFilters)
                        {
                            if (!string.IsNullOrEmpty(schoolName))
                            {
                                command.Parameters.AddWithValue("@SchoolName", schoolName.Trim().ToUpper());

                            }
                            if (!string.IsNullOrEmpty(division))
                            {
                                command.Parameters.AddWithValue("@Division", division.Trim().ToUpper());

                            }
                            if (!string.IsNullOrEmpty(region))
                            {
                                command.Parameters.AddWithValue("@Region", region.Trim().ToUpper());

                            }
                            if (!string.IsNullOrEmpty(district))
                            {
                                command.Parameters.AddWithValue("@District", district.Trim().ToUpper());

                            }
                        }
                        


                        var rowCount = 0;
                        
                        // Execute first query and read results
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    rowCount++;
                                    
                                    if (reader.IsDBNull("Level"))
                                    {
                                        _logger.LogWarning("Row {Row}: Level is NULL, skipping", rowCount);
                                        continue;
                                    }
                                    
                                    var level = reader.GetString("Level");
                                    var count = reader.IsDBNull("Count") ? 0 : reader.GetInt32("Count");
                                    
                                    _logger.LogInformation("Found Level: '{Level}' with Count: {Count}", level, count);

                                    // Match level values (handle variations)
                                    var levelUpper = level.Trim().ToUpper();
                                    
                                    if (levelUpper == "MINOR")
                                    {
                                        minorCount = count;
                                    }
                                    else if (levelUpper == "MAJOR")
                                    {
                                        majorCount = count;
                                    }
                                    else if (levelUpper == "PROHIBITED" || levelUpper == "PROHIBITED ACTS" || 
                                             levelUpper.Contains("PROHIBITED"))
                                    {
                                        prohibitedCount = count;
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Unknown LevelOfOffense value: '{Level}' (Count: {Count})", level, count);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error reading row {Row}: {Message}", rowCount, ex.Message);
                                }
                            }
                        } // Reader is now closed/disposed
                        
                        _logger.LogInformation("Query returned {RowCount} rows for {Month}", rowCount, monthLabel);

                        // If no results and we have location filters, try without filters as fallback
                        // IMPORTANT: Reader must be closed before executing new command
                        // Fallback logic removed - SimplifiedIncidentReports is the single source of truth.
                        if (rowCount == 0)
                        {
                            _logger.LogInformation("No trend data found for {Month}", monthLabel);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ERROR processing month {Month}: {Message}\nStack Trace: {StackTrace}", 
                            monthLabel, ex.Message, ex.StackTrace);
                        // Continue to next month
                    }

                    // Add data for this month (even if all zeros)
                    trendData.Add(new CategoryTrendData
                    {
                        Label = monthLabel,
                        MinorCount = minorCount,
                        MajorCount = majorCount,
                        ProhibitedCount = prohibitedCount,
                        Year = month.Year,
                        Month = month.Month
                    });

                    _logger.LogInformation("Month {Month} result: Minor={Minor}, Major={Major}, Prohibited={Prohibited}", 
                        monthLabel, minorCount, majorCount, prohibitedCount);
                }

                _logger.LogInformation("Successfully retrieved category trend data for {Count} months", trendData.Count);
                
                // Log summary
                var totalMinor = trendData.Sum(t => t.MinorCount);
                var totalMajor = trendData.Sum(t => t.MajorCount);
                var totalProhibited = trendData.Sum(t => t.ProhibitedCount);
                _logger.LogInformation("Summary - Total Minor: {Minor}, Total Major: {Major}, Total Prohibited: {Prohibited}", 
                    totalMinor, totalMajor, totalProhibited);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL ERROR in GetCategoryTrendDataAsync: {Message}\nStack Trace: {StackTrace}", 
                    ex.Message, ex.StackTrace);
            }

            // Ensure we always return 12 months of data
            if (trendData.Count < 12)
            {
                var now = DateTime.Now;
                var currentYear = now.Year;
                var last12Months = Enumerable.Range(1, 12).Select(month => new DateTime(currentYear, month, 1)).ToList();
                
                foreach (var month in last12Months)
                {
                    var monthLabel = month.ToString("MMMM");
                    if (!trendData.Any(t => t.Year == month.Year && t.Month == month.Month))
                    {
                        trendData.Add(new CategoryTrendData
                        {
                            Label = monthLabel,
                            MinorCount = 0,
                            MajorCount = 0,
                            ProhibitedCount = 0,
                            Year = month.Year,
                            Month = month.Month
                        });
                    }
                }
                
                // Sort by chronological order (Year, then Month) to maintain proper sequence
                trendData = trendData.OrderBy(t => t.Year).ThenBy(t => t.Month).ToList();
            }

            // Sort by chronological order (last 12 months from oldest to newest)
            // This ensures the chart shows a proper timeline
            trendData = trendData.OrderBy(t => t.Year).ThenBy(t => t.Month).ToList();

            return trendData;
        }

        public async Task<PODTeacher?> AuthenticatePODAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("Authenticating POD user: {Username}", username);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Query to authenticate POD user
                var query = @"
                    SELECT t.TeacherID, t.TeacherName, t.Position, t.SchoolID,
                           COALESCE(s.SchoolName, t.SchoolName, 'Koronadal National Comprehensive High School') as SchoolName,
                           COALESCE(s.Division, 'Koronadal City Division') as Division,
                           COALESCE(s.Region, 'Region XII (SOCCSKSARGEN)') as Region,
                           COALESCE(s.District, 'Koronadal City District') as District
                    FROM teachers t
                    INNER JOIN users u ON t.UserID = u.UserID
                    LEFT JOIN schools s ON t.SchoolID = s.SchoolID
                    WHERE u.Username = @Username 
                    AND u.Password = @Password 
                    AND t.Position = 'POD' 
                    AND t.IsActive = 1
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var podInfo = new PODTeacher
                    {
                        TeacherID = reader.GetInt32("TeacherID"),
                        TeacherName = reader.GetString("TeacherName"),
                        Position = reader.GetString("Position"),
                        SchoolName = reader.IsDBNull("SchoolName") ? "" : reader.GetString("SchoolName"),
                        SchoolID = reader.IsDBNull("SchoolID") ? "" : reader.GetInt32("SchoolID").ToString(),
                        Division = reader.IsDBNull("Division") ? "" : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? "" : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? "" : reader.GetString("District")
                    };

                    _logger.LogInformation("POD authentication successful for {Username} - School: {SchoolName}", username, podInfo.SchoolName);
                    return podInfo;
                }

                _logger.LogWarning("POD authentication failed for {Username} - Invalid credentials or not a POD", username);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating POD user {Username}: {Message}", username, ex.Message);
                return null;
            }
        }

        public async Task<List<Studentclass>> GetStudentsBySchoolAsync(string schoolName)
        {
            var students = new List<Studentclass>();

            try
            {
                _logger.LogInformation("Getting students for school: {SchoolName}", schoolName);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT s.StudentID, s.UserID, s.StudentName, s.Gender, s.Section, s.GradeLevel, s.Strand,
                           s.SchoolYear, s.ParentName, s.ParentContact, s.TeacherID, s.IsActive, s.DateRegister,
                           s.SchoolID, s.SchoolName, s.School_ID
                    FROM students s
                    LEFT JOIN schools sc ON s.SchoolID = sc.SchoolID
                    WHERE s.IsActive = 1
                    AND (sc.SchoolName = @SchoolName OR s.SchoolName = @SchoolName)
                    ORDER BY s.StudentName";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@SchoolName", schoolName);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var student = new Studentclass
                    {
                        StudentID = reader.GetInt32("StudentID"),
                        UserID = reader.GetInt32("UserID"),
                        StudentName = reader.GetString("StudentName"),
                        Gender = reader.IsDBNull("Gender") ? string.Empty : reader.GetString("Gender"),
                        Section = reader.IsDBNull("Section") ? null : reader.GetString("Section"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel"),
                        Strand = reader.IsDBNull("Strand") ? null : reader.GetString("Strand"),
                        SchoolYear = reader.IsDBNull("SchoolYear") ? null : reader.GetString("SchoolYear"),
                        ParentName = reader.IsDBNull("ParentName") ? null : reader.GetString("ParentName"),
                        ParentContact = reader.IsDBNull("ParentContact") ? null : reader.GetString("ParentContact"),
                        TeacherID = reader.IsDBNull("TeacherID") ? null : reader.GetInt32("TeacherID"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DateRegister = reader.GetDateTime("DateRegister"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        School_ID = reader.IsDBNull("School_ID") ? null : reader.GetString("School_ID")
                    };
                    students.Add(student);
                }

                _logger.LogInformation("Retrieved {Count} students from {SchoolName}", students.Count, schoolName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students for school {SchoolName}: {Message}", schoolName, ex.Message);
            }

            return students;
        }

        public async Task<IncidentReportModel?> GetIncidentReportByReferenceNumberAsync(string referenceNumber)
        {
            try
            {
                _logger.LogInformation("Getting incident report by reference number: {ReferenceNumber}", referenceNumber);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT sir.IncidentID, sir.FullName as ComplainantName, sir.RespondentName, sir.AdviserName, sir.VictimName, 
                           sir.IncidentType, sir.Description as IncidentDescription, sir.DateReported, 
                           sir.Status, sir.ReferenceNumber, sir.SchoolName, sir.Division, 
                           sir.EvidencePhotoBase64,
                           COALESCE(vt.ViolationCategory, 'Incident Report') as LevelOfOffense
                    FROM simplifiedincidentreports sir
                    LEFT JOIN violationtypes vt ON UPPER(TRIM(sir.IncidentType)) = UPPER(TRIM(vt.ViolationName)) AND (vt.IsActive = 1 OR vt.IsActive IS NULL)
                    WHERE sir.ReferenceNumber = @ReferenceNumber AND (sir.IsActive = 1 OR sir.IsActive IS NULL)
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ReferenceNumber", referenceNumber);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return new IncidentReportModel
                    {
                        IncidentID = reader.GetInt32("IncidentID"),
                        ComplainantName = reader.IsDBNull("ComplainantName") ? "" : reader.GetString("ComplainantName"),
                        ComplainantGrade = "", // Not in SimplifiedIncidentReports
                        ComplainantSection = "", // Not in SimplifiedIncidentReports
                        ComplainantStrand = "", // Not in SimplifiedIncidentReports
                        VictimName = reader.IsDBNull("VictimName") ? "" : reader.GetString("VictimName"),
                        VictimContact = "", // Not in SimplifiedIncidentReports
                        IncidentType = reader.IsDBNull("IncidentType") ? "" : reader.GetString("IncidentType"),
                        OtherIncidentType = "", 
                        IncidentDescription = reader.IsDBNull("IncidentDescription") ? "" : reader.GetString("IncidentDescription"),
                        RespondentName = reader.IsDBNull("RespondentName") ? "" : reader.GetString("RespondentName"),
                        AdviserName = reader.IsDBNull("AdviserName") ? "" : reader.GetString("AdviserName"),
                        PODIncharge = "", 
                        DateReported = reader.GetDateTime("DateReported"),
                        EvidencePhotoBase64 = reader.IsDBNull("EvidencePhotoBase64") ? "" : reader.GetString("EvidencePhotoBase64"),
                        ReferenceNumber = reader.IsDBNull("ReferenceNumber") ? "" : reader.GetString("ReferenceNumber"),
                        Status = reader.IsDBNull("Status") ? "Pending" : reader.GetString("Status"),
                        SchoolName = reader.IsDBNull("SchoolName") ? "" : reader.GetString("SchoolName"),
                        Division = reader.IsDBNull("Division") ? "" : reader.GetString("Division")
                    };
                }

                _logger.LogWarning("No incident report found with reference number: {ReferenceNumber}", referenceNumber);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident report by reference number: {ReferenceNumber}", referenceNumber);
                return null;
            }
        }
        public async Task<OfficialIncidentReport> CreateOfficialIncidentReportAsync(OfficialIncidentReportRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new official incident report for ReporterID: {ReporterID} (Saving to Simplified table)", request.ReporterID);
                
                // Map to SimplifiedIncidentReportRequest
                // Fetch actual reporter name
                string reporterName = $"Official Report ({request.ReporterRole} ID: {request.ReporterID})"; // Default fallback

                try 
                {
                    if (string.Equals(request.ReporterRole, "Teacher", StringComparison.OrdinalIgnoreCase))
                    {
                        var connectionString = _dbConnections.GetConnection();
                        using var connection = new MySqlConnection(connectionString);
                        await connection.OpenAsync();
                        
                        var cmd = new MySqlCommand("SELECT TeacherName FROM teachers WHERE TeacherID = @ID", connection);
                        cmd.Parameters.AddWithValue("@ID", request.ReporterID);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null) reporterName = result.ToString();
                    }
                    else if (string.Equals(request.ReporterRole, "POD", StringComparison.OrdinalIgnoreCase))
                    {
                        reporterName = "POD Staff";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching reporter name for ID {ReporterID}", request.ReporterID);
                    // Continue with default/fallback name
                }

                // Map to SimplifiedIncidentReportRequest
                var simplifiedRequest = new SimplifiedIncidentReportRequest
                {
                    FullName = reporterName,
                    RespondentName = request.RespondentName,
                    AdviserName = request.AdviserName,
                    VictimName = request.VictimName,
                    IncidentType = request.IncidentType,
                    Description = request.Description,
                    Status = "Reported",
                    SchoolName = request.SchoolName,
                    Division = request.Division
                };

                var (id, refNum) = await CreateSimplifiedIncidentReportAsync(simplifiedRequest);

                return new OfficialIncidentReport
                {
                    OfficialIncidentID = id,
                    ReporterID = request.ReporterID,
                    ReporterRole = request.ReporterRole,
                    RespondentName = request.RespondentName,
                    AdviserName = request.AdviserName,
                    IncidentType = request.IncidentType,
                    Description = request.Description,
                    DateReported = DateTime.Now,
                    SchoolName = request.SchoolName,
                    Division = request.Division,
                    Status = "Reported",
                    ReferenceNumber = refNum
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating official incident report via simplified table");
                throw;
            }
        }

        public async Task<List<OfficialIncidentReport>> GetOfficialIncidentReportsAsync(int? reporterId = null, string? schoolName = null)
        {
            var reports = new List<OfficialIncidentReport>();
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT sir.IncidentID as OfficialIncidentID, 0 as ReporterID, 'System' as ReporterRole,
                           sir.RespondentName, sir.AdviserName, sir.IncidentType, sir.Description, 
                           sir.DateReported, sir.SchoolName, sir.Division, sir.Status,
                           'System' as ReporterName
                    FROM simplifiedincidentreports sir
                    WHERE (sir.IsActive = 1 OR sir.IsActive IS NULL)";

                if (reporterId.HasValue)
                {
                    query += " AND oir.ReporterID = @ReporterID";
                }

                if (!string.IsNullOrEmpty(schoolName))
                {
                    query += " AND oir.SchoolName = @SchoolName";
                }

                query += " ORDER BY oir.DateReported DESC";

                using var command = new MySqlCommand(query, connection);
                if (reporterId.HasValue)
                {
                    command.Parameters.AddWithValue("@ReporterID", reporterId.Value);
                }
                if (!string.IsNullOrEmpty(schoolName))
                {
                    command.Parameters.AddWithValue("@SchoolName", schoolName);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    reports.Add(new OfficialIncidentReport
                    {
                        OfficialIncidentID = reader.GetInt32("OfficialIncidentID"),
                        ReporterID = reader.GetInt32("ReporterID"),
                        ReporterRole = reader.GetString("ReporterRole"),
                        RespondentName = reader.GetString("RespondentName"),
                        AdviserName = reader.IsDBNull("AdviserName") ? "" : reader.GetString("AdviserName"),
                        IncidentType = reader.GetString("IncidentType"),
                        Description = reader.GetString("Description"),
                        DateReported = reader.GetDateTime("DateReported"),
                        SchoolName = reader.GetString("SchoolName"),
                        Division = reader.GetString("Division"),
                        Status = reader.GetString("Status"),
                        ReporterName = reader.IsDBNull("ReporterName") ? "System" : reader.GetString("ReporterName")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting official incident reports");
            }
            return reports;
        }
        public async Task<List<OfficialIncidentReportSummary>> GetOfficialIncidentReportSummaryAsync(string? schoolName = null)
        {
            var summaries = new List<OfficialIncidentReportSummary>();
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        0 as ReporterID, 
                        'System' as ReporterName, 
                        'System' as ReporterRole, 
                        COUNT(*) as TotalReports, 
                        MAX(sir.DateReported) as LatestReportDate,
                        sir.SchoolName
                    FROM simplifiedincidentreports sir
                    WHERE (sir.IsActive = 1 OR sir.IsActive IS NULL)";

                if (!string.IsNullOrEmpty(schoolName))
                {
                    query += " AND sir.SchoolName = @SchoolName";
                }

                query += " GROUP BY sir.SchoolName";
                query += " ORDER BY TotalReports DESC";

                using var command = new MySqlCommand(query, connection);
                if (!string.IsNullOrEmpty(schoolName))
                {
                    command.Parameters.AddWithValue("@SchoolName", schoolName);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    summaries.Add(new OfficialIncidentReportSummary
                    {
                        ReporterID = reader.GetInt32("ReporterID"),
                        ReporterName = reader.IsDBNull("ReporterName") ? "Unknown" : reader.GetString("ReporterName"),
                        ReporterRole = reader.GetString("ReporterRole"),
                        TotalReports = reader.GetInt32("TotalReports"),
                        LatestReportDate = reader.GetDateTime("LatestReportDate"),
                        SchoolName = reader.GetString("SchoolName")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting official incident report summary");
            }
            return summaries;
        }

        public async Task<List<OfficialIncidentReport>> GetOfficialIncidentReportsByReporterAsync(int reporterId)
        {
            var reports = new List<OfficialIncidentReport>();
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT sir.IncidentID as OfficialIncidentID, sir.ReporterID, sir.ReporterRole, 
                           sir.RespondentName, sir.AdviserName, sir.IncidentType, sir.Description, 
                           sir.DateReported, sir.SchoolName, sir.Division, sir.Status, 
                           (COALESCE(sir.IsActive, 1) = 1) as IsActive, sir.ReferenceNumber
                    FROM simplifiedincidentreports sir
                    WHERE sir.ReporterID = @ReporterID AND (sir.IsActive = 1 OR sir.IsActive IS NULL) 
                    ORDER BY sir.DateReported DESC";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ReporterID", reporterId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    reports.Add(new OfficialIncidentReport
                    {
                        OfficialIncidentID = reader.GetInt32("OfficialIncidentID"),
                        ReporterID = reader.GetInt32("ReporterID"),
                        ReporterRole = reader.GetString("ReporterRole"),
                        RespondentName = reader.GetString("RespondentName"),
                        AdviserName = reader.IsDBNull("AdviserName") ? "" : reader.GetString("AdviserName"),
                        IncidentType = reader.GetString("IncidentType"),
                        Description = reader.GetString("Description"),
                        DateReported = reader.GetDateTime("DateReported"),
                        SchoolName = reader.GetString("SchoolName"),
                        Division = reader.GetString("Division"),
                        Status = reader.GetString("Status"),
                        IsActive = reader.GetBoolean("IsActive"),
                        ReferenceNumber = reader.IsDBNull("ReferenceNumber") ? "" : reader.GetString("ReferenceNumber")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting official incident reports for reporter {reporterId}");
            }
            return reports;
        }
        public async Task<string?> GetStudentAdviserByNameAsync(string studentName, string schoolName)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT t.TeacherName 
                    FROM students s
                    LEFT JOIN teachers t ON s.TeacherID = t.TeacherID
                    WHERE (s.StudentName = @StudentName OR s.StudentName LIKE CONCAT('%', @StudentName, '%'))
                      AND s.SchoolName = @SchoolName 
                      AND s.IsActive = 1
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentName", studentName);
                command.Parameters.AddWithValue("@SchoolName", schoolName);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student adviser for {StudentName}", studentName);
                return null;
            }
        }
        public async Task<List<ViolationType>> GetViolationTypesAsync()
        {
            var violations = new List<ViolationType>();
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ViolationID, ViolationName, ViolationCategory, HasVictim, IsActive 
                    FROM violationtypes 
                    WHERE IsActive = 1
                    ORDER BY ViolationCategory, ViolationName";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    violations.Add(new ViolationType
                    {
                        ViolationID = reader.GetInt32("ViolationID"),
                        ViolationName = reader.GetString("ViolationName"),
                        ViolationCategory = reader.GetString("ViolationCategory"),
                        HasVictim = reader.IsDBNull("HasVictim") ? "Yes" : reader.GetString("HasVictim"),
                        IsActive = reader.GetBoolean("IsActive")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting violation types");
            }
            return violations;
        }

        public async Task<List<StudentWithCasesModel>> GetStudentsWithCasesByAdviserAsync(string adviserName)
        {
            var studentsWithCases = new List<StudentWithCasesModel>();

            try
            {
                _logger.LogInformation("Getting students with cases for adviser: {AdviserName}", adviserName);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        sir.RespondentName as StudentName,
                        sir.AdviserName,
                        COUNT(*) as TotalCases,
                        SUM(CASE WHEN vt.ViolationCategory = 'Minor' THEN 1 ELSE 0 END) as MinorCases,
                        SUM(CASE WHEN vt.ViolationCategory = 'Major' THEN 1 ELSE 0 END) as MajorCases,
                        SUM(CASE WHEN vt.ViolationCategory = 'Prohibited Acts' THEN 1 ELSE 0 END) as ProhibitedCases,
                        SUM(CASE WHEN sir.Status NOT IN ('Resolved', 'Closed') THEN 1 ELSE 0 END) as ActiveCases,
                        MAX(sir.DateReported) as LatestIncidentDate
                    FROM simplifiedincidentreports sir
                    LEFT JOIN violationtypes vt ON UPPER(TRIM(sir.IncidentType)) = UPPER(TRIM(vt.ViolationName)) AND (vt.IsActive = 1 OR vt.IsActive IS NULL)
                    WHERE UPPER(TRIM(sir.AdviserName)) = UPPER(TRIM(@AdviserName))
                        AND (sir.IsActive = 1 OR sir.IsActive IS NULL)
                        AND sir.RespondentName IS NOT NULL
                        AND sir.RespondentName != ''
                    GROUP BY sir.RespondentName, sir.AdviserName
                    ORDER BY ActiveCases DESC, TotalCases DESC, sir.RespondentName";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdviserName", adviserName);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var student = new StudentWithCasesModel
                    {
                        StudentName = reader.GetString("StudentName"),
                        AdviserName = reader.GetString("AdviserName"),
                        TotalCases = reader.GetInt32("TotalCases"),
                        MinorCases = reader.GetInt32("MinorCases"),
                        MajorCases = reader.GetInt32("MajorCases"),
                        ProhibitedCases = reader.GetInt32("ProhibitedCases"),
                        ActiveCases = reader.GetInt32("ActiveCases"),
                        LatestIncidentDate = reader.GetDateTime("LatestIncidentDate")
                    };
                    studentsWithCases.Add(student);
                    _logger.LogInformation("Found student with cases: {StudentName} - Total: {TotalCases}, Active: {ActiveCases}", 
                        student.StudentName, student.TotalCases, student.ActiveCases);
                }

                _logger.LogInformation("Retrieved {Count} students with cases for adviser {AdviserName}", studentsWithCases.Count, adviserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students with cases for adviser {AdviserName}", adviserName);
            }

            return studentsWithCases;
        }

        /// <summary>Students with cases + full case records (violations) in one call from SimplifiedIncidentReports — same source as "3 Minor" count.</summary>
        public async Task<List<StudentWithCasesAndDetailsModel>> GetStudentsWithCasesAndDetailsByAdviserAsync(string adviserName)
        {
            var rows = new List<(string RespondentName, string AdviserName, DateTime DateReported, string IncidentType, string LevelOfOffense, string Status)>();

            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        sir.RespondentName,
                        sir.AdviserName,
                        sir.DateReported,
                        sir.IncidentType,
                        COALESCE(vt.ViolationCategory, 'Minor') AS LevelOfOffense,
                        COALESCE(sir.Status, 'Active') AS Status
                    FROM simplifiedincidentreports sir
                    LEFT JOIN violationtypes vt ON UPPER(TRIM(sir.IncidentType)) = UPPER(TRIM(vt.ViolationName)) AND (vt.IsActive = 1 OR vt.IsActive IS NULL)
                    WHERE UPPER(TRIM(sir.AdviserName)) = UPPER(TRIM(@AdviserName))
                        AND (sir.IsActive = 1 OR sir.IsActive IS NULL)
                        AND sir.RespondentName IS NOT NULL
                        AND sir.RespondentName != ''
                    ORDER BY sir.RespondentName, sir.DateReported DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdviserName", adviserName);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add((
                        reader.GetString("RespondentName"),
                        reader.GetString("AdviserName"),
                        reader.GetDateTime("DateReported"),
                        reader.IsDBNull("IncidentType") ? "Unknown" : reader.GetString("IncidentType"),
                        reader.IsDBNull("LevelOfOffense") ? "Minor" : reader.GetString("LevelOfOffense"),
                        reader.IsDBNull("Status") ? "Active" : reader.GetString("Status")
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students with cases and details for adviser {AdviserName}", adviserName);
                return new List<StudentWithCasesAndDetailsModel>();
            }

            var grouped = rows
                .GroupBy(r => (r.RespondentName, r.AdviserName))
                .Select(g =>
                {
                    var cases = g.Select(r => new StudentCaseRecordDto
                    {
                        DateOfOffense = r.DateReported,
                        ViolationCommitted = r.IncidentType,
                        LevelOfOffense = r.LevelOfOffense,
                        Status = r.Status
                    }).ToList();
                    var minor = cases.Count(c => c.LevelOfOffense != null && c.LevelOfOffense.Contains("Minor", StringComparison.OrdinalIgnoreCase));
                    var major = cases.Count(c => c.LevelOfOffense != null && c.LevelOfOffense.Contains("Major", StringComparison.OrdinalIgnoreCase));
                    var prohibited = cases.Count(c => c.LevelOfOffense != null && (c.LevelOfOffense.Contains("Prohibited", StringComparison.OrdinalIgnoreCase) || c.LevelOfOffense.Contains("Grave", StringComparison.OrdinalIgnoreCase)));
                    var active = cases.Count(c => c.Status != null && !c.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) && !c.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase));
                    return new StudentWithCasesAndDetailsModel
                    {
                        StudentName = g.Key.RespondentName,
                        AdviserName = g.Key.AdviserName,
                        TotalCases = cases.Count,
                        MinorCases = minor,
                        MajorCases = major,
                        ProhibitedCases = prohibited,
                        ActiveCases = active,
                        LatestIncidentDate = g.Max(r => r.DateReported),
                        Cases = cases
                    };
                })
                .OrderByDescending(s => s.ActiveCases)
                .ThenByDescending(s => s.TotalCases)
                .ThenBy(s => s.StudentName)
                .ToList();

            _logger.LogInformation("Retrieved {Count} students with case details for adviser {AdviserName}", grouped.Count, adviserName);
            return grouped;
        }
        
        public async Task<List<object>> GetStudentCasesAsync(string studentName)
        {
            var cases = new List<object>();
            
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                
                var trimmedName = (studentName ?? "").Trim();
                var query = @"
                    SELECT 
                        s.IncidentReportID,
                        s.DateReported,
                        s.RespondentName,
                        s.IncidentType,
                        v.ViolationCategory AS LevelOfOffense,
                        s.Status
                    FROM simplifiedincidentreports s
                    LEFT JOIN violationtypes v ON UPPER(TRIM(s.IncidentType)) = UPPER(TRIM(v.ViolationName)) AND (v.IsActive = 1 OR v.IsActive IS NULL)
                    WHERE (s.IsActive = 1 OR s.IsActive IS NULL)
                      AND s.RespondentName IS NOT NULL
                      AND s.RespondentName != ''
                      AND (
                          UPPER(TRIM(s.RespondentName)) = UPPER(TRIM(@StudentName))
                          OR UPPER(TRIM(s.RespondentName)) LIKE CONCAT('%', UPPER(TRIM(@StudentName)), '%')
                      )
                    ORDER BY s.DateReported DESC";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentName", trimmedName);
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // Safe reading logic
                    var idObj = reader["IncidentReportID"];
                    var dateObj = reader["DateReported"];
                    var typeObj = reader["IncidentType"];
                    var levelObj = reader["LevelOfOffense"];
                    var statusObj = reader["Status"];

                    cases.Add(new
                    {
                        incidentReportID = (idObj == DBNull.Value) ? 0 : Convert.ToInt32(idObj),
                        dateOfOffense = (dateObj == DBNull.Value) ? DateTime.MinValue : Convert.ToDateTime(dateObj),
                        dateReported = (dateObj == DBNull.Value) ? DateTime.MinValue : Convert.ToDateTime(dateObj),
                        violationCommitted = (typeObj == DBNull.Value) ? "Unknown" : typeObj.ToString(),
                        incidentType = (typeObj == DBNull.Value) ? "Unknown" : typeObj.ToString(),
                        levelOfOffense = (levelObj == DBNull.Value) ? "Minor" : levelObj.ToString(),
                        status = (statusObj == DBNull.Value) ? "Active" : statusObj.ToString()
                    });
                }

                _logger.LogInformation("GetStudentCasesAsync: found {Count} cases for student '{StudentName}' (trimmed: '{TrimmedName}')", cases.Count, studentName, trimmedName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cases for student {StudentName}", studentName);
            }
            
            return cases;
        }

        public async Task<DashboardStatistics> GetDashboardStatsAsync(
            string? schoolName = null,
            string? division = null,
            string? region = null,
            string? district = null)
        {
            var stats = new DashboardStatistics();

            try
            {
                _logger.LogInformation("=== GETTING DASHBOARD STATS ===");
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Base WHERE clause for location filtering
                var whereClause = "WHERE (sir.IsActive = 1 OR sir.IsActive IS NULL)";
                var hasLocation = false;

                var joinClause = "LEFT JOIN schools s ON TRIM(UPPER(sir.SchoolName)) = TRIM(UPPER(s.SchoolName))";

                if (!string.IsNullOrEmpty(schoolName)) 
                { 
                    whereClause += " AND TRIM(UPPER(sir.SchoolName)) = TRIM(UPPER(@SchoolName))"; 
                    hasLocation = true; 
                }
                else if (!string.IsNullOrEmpty(division))
                {
                    whereClause += " AND (TRIM(UPPER(sir.Division)) LIKE CONCAT('%', TRIM(UPPER(@Division)), '%') OR TRIM(UPPER(s.Division)) LIKE CONCAT('%', TRIM(UPPER(@Division)), '%'))";
                    hasLocation = true;
                }
                else if (!string.IsNullOrEmpty(region))
                {
                    whereClause += " AND (TRIM(UPPER(s.Region)) = @Region)";
                    hasLocation = true;
                }
                else if (!string.IsNullOrEmpty(district))
                {
                    whereClause += " AND (TRIM(UPPER(s.District)) = @District)";
                    hasLocation = true;
                }

                // 1. Status Counts
                var statusQuery = $@"
                    SELECT sir.Status, COUNT(*) as Count
                    FROM simplifiedincidentreports sir
                    {joinClause}
                    {whereClause}
                    GROUP BY sir.Status";
                
                using (var cmd = new MySqlCommand(statusQuery, connection))
                {
                    AddLocationParams(cmd, schoolName, division, region, district);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull("Status"))
                        {
                            stats.StatusCounts.Add(new StatusStat 
                            { 
                                Status = reader.GetString("Status"), 
                                Count = reader.GetInt32("Count") 
                            });
                        }
                    }
                }

                // 2. Top Behaviors (Incident Types)
                var behaviorWhere = whereClause + " AND sir.Status != 'Rejected'";
                var behaviorQuery = $@"
                    SELECT sir.IncidentType, COUNT(*) as Count
                    FROM simplifiedincidentreports sir
                    {joinClause}
                    {behaviorWhere}
                    AND sir.IncidentType IS NOT NULL AND sir.IncidentType != ''
                    GROUP BY sir.IncidentType
                    ORDER BY Count DESC
                    LIMIT 5";

                using (var cmd = new MySqlCommand(behaviorQuery, connection))
                {
                    AddLocationParams(cmd, schoolName, division, region, district);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        stats.TopBehaviors.Add(new BehaviorStat 
                        { 
                            Behavior = reader.GetString("IncidentType"), 
                            Count = reader.GetInt32("Count") 
                        });
                    }
                }

                // 3. Yearly Stats
                var yearlyQuery = $@"
                    SELECT YEAR(sir.DateReported) as Year, COUNT(*) as Count
                    FROM simplifiedincidentreports sir
                    {joinClause}
                    {whereClause}
                    GROUP BY YEAR(sir.DateReported)
                    ORDER BY Year";

                using (var cmd = new MySqlCommand(yearlyQuery, connection))
                {
                    AddLocationParams(cmd, schoolName, division, region, district);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        stats.YearlyStats.Add(new YearlyStat 
                        { 
                            Year = reader.GetInt32("Year").ToString(), 
                            Count = reader.GetInt32("Count") 
                        });
                    }
                }

                // 4. Weekly Stats (Current Month)
                var now = DateTime.Now;
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                var datesQuery = $@"
                    SELECT sir.DateReported
                    FROM simplifiedincidentreports sir
                    {joinClause}
                    {whereClause}
                    AND sir.DateReported >= @MonthStart AND sir.DateReported <= @MonthEnd";

                using (var cmd = new MySqlCommand(datesQuery, connection))
                {
                    AddLocationParams(cmd, schoolName, division, region, district);
                    cmd.Parameters.AddWithValue("@MonthStart", monthStart);
                    cmd.Parameters.AddWithValue("@MonthEnd", monthEnd);

                    var dates = new List<DateTime>();
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        dates.Add(reader.GetDateTime("DateReported"));
                    }

                    var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                    var weekGroups = dates
                        .GroupBy(d => cal.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                        .OrderBy(g => g.Key)
                        .ToList();

                    var firstWeekNum = cal.GetWeekOfYear(monthStart, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    var lastWeekNum = cal.GetWeekOfYear(monthEnd, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);

                    int weekIndex = 1;
                    
                    for (int w = firstWeekNum; w <= lastWeekNum; w++)
                    {
                        var count = weekGroups.FirstOrDefault(g => g.Key == w)?.Count() ?? 0;
                        stats.WeeklyStats.Add(new WeeklyStat { Label = $"Week {weekIndex}", Count = count });
                        weekIndex++;
                    }
                    
                    if (!stats.WeeklyStats.Any())
                    {
                        stats.WeeklyStats.Add(new WeeklyStat { Label = "Week 1", Count = 0 });
                    }
                }

                _logger.LogInformation("Dashboard stats retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
            }

            return stats;
        }

        private void AddLocationParams(MySqlCommand cmd, string? school, string? div, string? reg, string? dist)
        {
            if (!string.IsNullOrEmpty(school)) cmd.Parameters.AddWithValue("@SchoolName", school);
            if (!string.IsNullOrEmpty(div)) cmd.Parameters.AddWithValue("@Division", div);
            if (!string.IsNullOrEmpty(reg)) cmd.Parameters.AddWithValue("@Region", reg);
            if (!string.IsNullOrEmpty(dist)) cmd.Parameters.AddWithValue("@District", dist);
        }
    }
}
