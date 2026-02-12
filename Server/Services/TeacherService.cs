using MySql.Data.MySqlClient;
using SharedProject;
using Server.Data;
using System.Data;

namespace Server.Services
{
    public class TeacherService
    {
        private readonly Dbconnections _dbConnections;

        public TeacherService(Dbconnections dbConnections)
        {
            _dbConnections = dbConnections;
        }

        public async Task<ApiResponse<int>> RegisterTeacherAsync(TeacherRegistrationRequest request)
        {
            try
            {
                Console.WriteLine($"RegisterTeacherAsync called with request: {System.Text.Json.JsonSerializer.Serialize(request)}");

                // Validate required fields
                if (string.IsNullOrEmpty(request.TeacherName))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Teacher name is required",
                        Errors = new List<string> { "Teacher name cannot be empty" }
                    };
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Email is required",
                        Errors = new List<string> { "Email cannot be empty" }
                    };
                }

                if (string.IsNullOrEmpty(request.Username))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Username is required",
                        Errors = new List<string> { "Username cannot be empty" }
                    };
                }

                if (string.IsNullOrEmpty(request.Password))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Password is required",
                        Errors = new List<string> { "Password cannot be empty" }
                    };
                }

                if (string.IsNullOrEmpty(request.Position))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Position is required",
                        Errors = new List<string> { "Position cannot be empty" }
                    };
                }

                // Check if username already exists
                var usernameExists = await CheckUsernameExistsAsync(request.Username);
                if (usernameExists)
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Username already exists",
                        Errors = new List<string> { "Username is already taken" }
                    };
                }

                // Check if email already exists
                var emailExists = await CheckEmailExistsAsync(request.Email);
                if (emailExists)
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Email already exists",
                        Errors = new List<string> { "Email is already registered" }
                    };
                }

                // Save password as plain text (as requested by user)
                var plainPassword = request.Password;

                // Convert to uppercase as required (except username)
                request.TeacherName = request.TeacherName.ToUpper();
                // Username should remain as entered by user


                // Start transaction
                using var connection = new MySqlConnection(_dbConnections.GetConnection());
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Insert into Users table
                    var userQuery = @"
                        INSERT INTO users (Username, Password, UserRole, IsActive, DateCreated)
                        VALUES (@Username, @Password, @UserRole, @IsActive, @DateCreated)";

                    var userParameters = new[]
                    {
                        new MySqlParameter("@Username", request.Username),
                        new MySqlParameter("@Password", plainPassword),
                        new MySqlParameter("@UserRole", "teacher"),
                        new MySqlParameter("@IsActive", true),
                        new MySqlParameter("@DateCreated", DateTime.Now)
                    };

                    var userCommand = new MySqlCommand(userQuery, connection, transaction);
                    userCommand.Parameters.AddRange(userParameters);
                    await userCommand.ExecuteScalarAsync();

                    // Get the inserted user ID
                    var getUserIdQuery = "SELECT UserID FROM users WHERE Username = @Username";
                    var getUserIdCommand = new MySqlCommand(getUserIdQuery, connection, transaction);
                    getUserIdCommand.Parameters.Add(new MySqlParameter("@Username", request.Username));
                    var insertedUserId = await getUserIdCommand.ExecuteScalarAsync();

                    if (insertedUserId == null)
                    {
                        throw new Exception("Failed to get user ID after insertion");
                    }

                    // Insert into Teachers table
                    var teacherQuery = @"
                        INSERT INTO teachers (UserID, TeacherName, Email, PhoneNumber, Position, Gender, SchoolID, SchoolName, School_ID, GradeLevel, Section, Strand, IsActive, DateRegister)
                        VALUES (@UserID, @TeacherName, @Email, @PhoneNumber, @Position, @Gender, @SchoolID, @SchoolName, @School_ID, @GradeLevel, @Section, @Strand, @IsActive, @DateRegister)";

                    var teacherParameters = new[]
                    {
                        new MySqlParameter("@UserID", insertedUserId),
                        new MySqlParameter("@TeacherName", request.TeacherName),
                        new MySqlParameter("@Email", request.Email),
                        new MySqlParameter("@PhoneNumber", request.PhoneNumber ?? (object)DBNull.Value),
                        new MySqlParameter("@Position", request.Position),
                        new MySqlParameter("@Gender", request.Gender ?? (object)DBNull.Value),
                        new MySqlParameter("@SchoolID", request.SchoolID ?? (object)DBNull.Value),
                        new MySqlParameter("@SchoolName", request.SchoolName ?? (object)DBNull.Value),
                        new MySqlParameter("@School_ID", request.School_ID ?? (object)DBNull.Value),
                        new MySqlParameter("@GradeLevel", request.GradeLevel ?? (object)DBNull.Value),
                        new MySqlParameter("@Section", request.Section ?? (object)DBNull.Value),
                        new MySqlParameter("@Strand", request.Strand ?? (object)DBNull.Value),
                        new MySqlParameter("@IsActive", true),
                        new MySqlParameter("@DateRegister", DateTime.Now)
                    };

                    var teacherCommand = new MySqlCommand(teacherQuery, connection, transaction);
                    teacherCommand.Parameters.AddRange(teacherParameters);
                    await teacherCommand.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    return new ApiResponse<int>
                    {
                        Success = true,
                        Message = "Teacher registered successfully",
                        Data = Convert.ToInt32(insertedUserId)
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Database transaction error in RegisterTeacherAsync: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = new List<string> { ex.Message }
                };
            }
        }


        public async Task<ApiResponse<int>> RegisterStudentAsync(StudentRegistrationRequest request)
        {
            try
            {
                Console.WriteLine($"RegisterStudentAsync called with request: {System.Text.Json.JsonSerializer.Serialize(request)}");

                // Validate required fields
                if (string.IsNullOrEmpty(request.StudentName))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Student name is required",
                        Errors = new List<string> { "Student name cannot be empty" }
                    };
                }

                // Auto-generate username (STUD + random numbers) and password
                string username;
                bool isUnique = false;
                var random = new Random();

                do 
                {
                    int randomNum = random.Next(1, 100000); // 1 to 99999
                    username = $"STUD{randomNum:D5}"; // e.g. STUD00123
                    isUnique = !await CheckUsernameExistsAsync(username);
                } while (!isUnique);

                var password = GeneratePassword();
                
                Console.WriteLine($"Generated unique username: {username}");
                Console.WriteLine($"Generated password: {password}");

                if (string.IsNullOrEmpty(request.Gender))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Gender is required",
                        Errors = new List<string> { "Gender cannot be empty" }
                    };
                }

                if (string.IsNullOrEmpty(request.GradeLevel))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Grade level is required",
                        Errors = new List<string> { "Grade level cannot be empty" }
                    };
                }

                if (string.IsNullOrEmpty(request.SchoolYear))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "School year is required",
                        Errors = new List<string> { "School year cannot be empty" }
                    };
                }

                // ParentName validation removed as requested by user

                // Username uniqueness is guaranteed by the generation loop above

                // Save password as plain text (as requested by user)
                var plainPassword = password;

                // Convert to uppercase as required (except username and password)
                request.StudentName = request.StudentName.ToUpper();
                // Username and password should remain as entered by user
                request.Section = request.Section?.ToUpper();
                if (request.ContactPerson == "Father")
                {
                    request.ParentName = request.FathersName;
                }
                else if (request.ContactPerson == "Mother")
                {
                    request.ParentName = request.MothersName;
                }
                else if (request.ContactPerson == "Guardian")
                {
                    request.ParentName = request.GuardianName;
                }

                request.ParentName = request.ParentName?.ToUpper();

                // Start transaction
                using var connection = new MySqlConnection(_dbConnections.GetConnection());
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    Console.WriteLine("Starting database transaction...");
                    
                    // Insert into Users table
                    var userQuery = @"
                        INSERT INTO users (Username, Password, UserRole, IsActive, DateCreated)
                        VALUES (@Username, @Password, @UserRole, @IsActive, @DateCreated)";

                    var userParameters = new[]
                    {
                        new MySqlParameter("@Username", username),
                        new MySqlParameter("@Password", plainPassword),
                        new MySqlParameter("@UserRole", "student"),
                        new MySqlParameter("@IsActive", true),
                        new MySqlParameter("@DateCreated", DateTime.Now)
                    };

                    var userCommand = new MySqlCommand(userQuery, connection, transaction);
                    userCommand.Parameters.AddRange(userParameters);
                    await userCommand.ExecuteScalarAsync();
                    Console.WriteLine("User inserted successfully");

                    // Get the inserted user ID
                    var getUserIdQuery = "SELECT UserID FROM users WHERE Username = @Username";
                    var getUserIdCommand = new MySqlCommand(getUserIdQuery, connection, transaction);
                    getUserIdCommand.Parameters.Add(new MySqlParameter("@Username", username));
                    var insertedUserId = await getUserIdCommand.ExecuteScalarAsync();
                    Console.WriteLine($"Inserted User ID: {insertedUserId}");

                    if (insertedUserId == null)
                    {
                        throw new Exception("Failed to get user ID after insertion");
                    }

                    // Insert into Students table
                    var studentQuery = @"
                        INSERT INTO students (UserID, StudentName, Gender, Section, GradeLevel, Strand, SchoolYear, ParentContact, SchoolID, SchoolName, School_ID, TeacherID, IsActive, DateRegister, FathersName, MothersName, GuardianName, GuardianContact, ContactPerson)
                        VALUES (@UserID, @StudentName, @Gender, @Section, @GradeLevel, @Strand, @SchoolYear, @ParentContact, @SchoolID, @SchoolName, @School_ID, @TeacherID, @IsActive, @DateRegister, @FathersName, @MothersName, @GuardianName, @GuardianContact, @ContactPerson)";

                    Console.WriteLine($"Executing student query: {studentQuery}");
                    Console.WriteLine($"SchoolID: {request.SchoolID}, SchoolName: {request.SchoolName}, School_ID: {request.School_ID}");

                    var studentParameters = new[]
                    {
                        new MySqlParameter("@UserID", insertedUserId),
                        new MySqlParameter("@StudentName", request.StudentName),
                        new MySqlParameter("@Gender", request.Gender),
                        new MySqlParameter("@Section", request.Section ?? (object)DBNull.Value),
                        new MySqlParameter("@GradeLevel", request.GradeLevel),
                        new MySqlParameter("@Strand", request.Strand ?? (object)DBNull.Value),
                        new MySqlParameter("@SchoolYear", request.SchoolYear),
                        new MySqlParameter("@ParentContact", request.ParentContact ?? (object)DBNull.Value),
                        new MySqlParameter("@SchoolID", request.SchoolID ?? (object)DBNull.Value),
                        new MySqlParameter("@SchoolName", request.SchoolName ?? (object)DBNull.Value),
                        new MySqlParameter("@School_ID", request.School_ID ?? (object)DBNull.Value),
                        new MySqlParameter("@TeacherID", request.TeacherID ?? (object)DBNull.Value),
                        new MySqlParameter("@IsActive", true),
                        new MySqlParameter("@DateRegister", DateTime.Now),
                        new MySqlParameter("@FathersName", request.FathersName ?? (object)DBNull.Value),
                        new MySqlParameter("@MothersName", request.MothersName ?? (object)DBNull.Value),
                        new MySqlParameter("@GuardianName", request.GuardianName ?? (object)DBNull.Value),
                        new MySqlParameter("@GuardianContact", request.GuardianContact ?? (object)DBNull.Value),
                        new MySqlParameter("@ContactPerson", request.ContactPerson ?? (object)DBNull.Value)
                    };

                    var studentCommand = new MySqlCommand(studentQuery, connection, transaction);
                    studentCommand.Parameters.AddRange(studentParameters);
                    await studentCommand.ExecuteNonQueryAsync();
                    Console.WriteLine("Student inserted successfully");

                    await transaction.CommitAsync();
                    Console.WriteLine("Transaction committed successfully");

                    return new ApiResponse<int>
                    {
                        Success = true,
                        Message = $"Student registered successfully! Username: {username}, Password: {password}",
                        Data = Convert.ToInt32(insertedUserId)
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Database transaction error in RegisterStudentAsync: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        private async Task<bool> CheckUsernameExistsAsync(string username)
        {
            using var connection = new MySqlConnection(_dbConnections.GetConnection());
            var query = "SELECT COUNT(1) FROM users WHERE Username = @Username";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.Add(new MySqlParameter("@Username", username));
            
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private async Task<bool> CheckEmailExistsAsync(string email)
        {
            using var connection = new MySqlConnection(_dbConnections.GetConnection());
            var query = "SELECT COUNT(1) FROM teachers WHERE Email = @Email";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.Add(new MySqlParameter("@Email", email));
            
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private bool IsValidPhilippinePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return true; // Allow empty phone numbers

            // Remove any non-digit characters
            phoneNumber = System.Text.RegularExpressions.Regex.Replace(phoneNumber, "[^0-9]", "");

            // Check if it's exactly 11 digits and starts with 09
            return phoneNumber.Length == 11 && phoneNumber.StartsWith("09");
        }

        // Password hashing removed - passwords are now stored as plain text as requested by user

        private bool IsAdminPosition(string position)
        {
            var adminPositions = new[] { "POD", "Guidance Counselor", "Principal", "Vice Principal" };
            return adminPositions.Contains(position);
        }

        // New method for schools dropdown data
        public async Task<ApiResponse<List<School>>> GetSchoolsAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnections.GetConnection());
                var query = "SELECT SchoolID, School_ID, SchoolName, Region, Division, District FROM schools WHERE IsActive = TRUE ORDER BY Region, Division, District, SchoolName";
                using var command = new MySqlCommand(query, connection);
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                var schools = new List<School>();
                while (await reader.ReadAsync())
                {
                    schools.Add(new School
                    {
                        SchoolID = reader.GetInt32("SchoolID"),
                        School_ID = reader.GetString("School_ID"),
                        SchoolName = reader.GetString("SchoolName"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District")
                    });
                }
                
                return new ApiResponse<List<School>>
                {
                    Success = true,
                    Message = "Schools retrieved successfully",
                    Data = schools
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<School>>
                {
                    Success = false,
                    Message = "Failed to retrieve schools",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // Student management methods for advisers
        public async Task<ApiResponse<List<Studentclass>>> GetStudentsByTeacherAsync(int teacherId)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnections.GetConnection());
                var query = @"
                    SELECT s.*, u.Username, u.Password 
                    FROM students s 
                    INNER JOIN users u ON s.UserID = u.UserID 
                    WHERE s.TeacherID = @TeacherID AND s.IsActive = 1 
                    ORDER BY s.StudentName";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.Add(new MySqlParameter("@TeacherID", teacherId));
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                var students = new List<Studentclass>();
                while (await reader.ReadAsync())
                {
                    students.Add(new Studentclass
                    {
                        StudentID = reader.GetInt32("StudentID"),
                        UserID = reader.GetInt32("UserID"),
                        StudentName = reader.GetString("StudentName"),
                        Gender = reader.GetString("Gender"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel"),
                        Section = reader.IsDBNull("Section") ? null : reader.GetString("Section"),
                        Strand = reader.IsDBNull("Strand") ? null : reader.GetString("Strand"),
                        SchoolYear = reader.IsDBNull("SchoolYear") ? null : reader.GetString("SchoolYear"),
                        ParentName = reader.IsDBNull("ParentName") ? null : reader.GetString("ParentName"),
                        ParentContact = reader.IsDBNull("ParentContact") ? null : reader.GetString("ParentContact"),
                        TeacherID = reader.IsDBNull("TeacherID") ? null : reader.GetInt32("TeacherID"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DateRegister = reader.GetDateTime("DateRegister"),
                        Username = reader.IsDBNull("Username") ? null : reader.GetString("Username"),
                        Password = reader.IsDBNull("Password") ? null : reader.GetString("Password"),
                        ContactPerson = reader.IsDBNull("ContactPerson") ? null : reader.GetString("ContactPerson"),
                        FathersName = reader.IsDBNull("FathersName") ? null : reader.GetString("FathersName"),
                        MothersName = reader.IsDBNull("MothersName") ? null : reader.GetString("MothersName"),
                        GuardianName = reader.IsDBNull("GuardianName") ? null : reader.GetString("GuardianName"),
                        GuardianContact = reader.IsDBNull("GuardianContact") ? null : reader.GetString("GuardianContact")
                    });
                }
                
                return new ApiResponse<List<Studentclass>>
                {
                    Success = true,
                    Message = "Students retrieved successfully",
                    Data = students
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Studentclass>>
                {
                    Success = false,
                    Message = "Failed to retrieve students",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<int>> AddStudentAsync(Studentclass student)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnections.GetConnection());
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Generate username from student name
                    var username = GenerateUsername(student.StudentName);
                    var password = "student123"; // Default password

                    // Insert into Users table
                    var userQuery = @"
                        INSERT INTO users (Username, Password, UserRole, IsActive, DateCreated)
                        VALUES (@Username, @Password, @UserRole, @IsActive, @DateCreated)";

                    var userCommand = new MySqlCommand(userQuery, connection, transaction);
                    userCommand.Parameters.AddRange(new[]
                    {
                        new MySqlParameter("@Username", username),
                        new MySqlParameter("@Password", password),
                        new MySqlParameter("@UserRole", "student"),
                        new MySqlParameter("@IsActive", true),
                        new MySqlParameter("@DateCreated", DateTime.Now)
                    });

                    await userCommand.ExecuteScalarAsync();

                    // Get the inserted user ID
                    var getUserIdQuery = "SELECT UserID FROM users WHERE Username = @Username";
                    var getUserIdCommand = new MySqlCommand(getUserIdQuery, connection, transaction);
                    getUserIdCommand.Parameters.Add(new MySqlParameter("@Username", username));
                    var insertedUserId = await getUserIdCommand.ExecuteScalarAsync();

                    if (insertedUserId == null)
                    {
                        throw new Exception("Failed to get user ID after insertion");
                    }

                    // Insert into Students table
                    var studentQuery = @"
                        INSERT INTO students (UserID, StudentName, Gender, GradeLevel, Section, Strand, SchoolYear, ParentContact, TeacherID, IsActive, DateRegister, FathersName, MothersName, GuardianName, GuardianContact, ContactPerson)
                        VALUES (@UserID, @StudentName, @Gender, @GradeLevel, @Section, @Strand, @SchoolYear, @ParentContact, @TeacherID, @IsActive, @DateRegister, @FathersName, @MothersName, @GuardianName, @GuardianContact, @ContactPerson)";

                    var studentCommand = new MySqlCommand(studentQuery, connection, transaction);
                    studentCommand.Parameters.AddRange(new[]
                    {
                        new MySqlParameter("@UserID", insertedUserId),
                        new MySqlParameter("@StudentName", student.StudentName.ToUpper()),
                        new MySqlParameter("@Gender", student.Gender),
                        new MySqlParameter("@GradeLevel", student.GradeLevel ?? (object)DBNull.Value),
                        new MySqlParameter("@Section", student.Section ?? (object)DBNull.Value),
                        new MySqlParameter("@Strand", student.Strand ?? (object)DBNull.Value),
                        new MySqlParameter("@SchoolYear", student.SchoolYear ?? (object)DBNull.Value),
                        new MySqlParameter("@ParentContact", student.ParentContact ?? (object)DBNull.Value),
                        new MySqlParameter("@TeacherID", student.TeacherID ?? (object)DBNull.Value),
                        new MySqlParameter("@IsActive", student.IsActive),
                        new MySqlParameter("@DateRegister", DateTime.Now),
                        new MySqlParameter("@FathersName", student.FathersName ?? (object)DBNull.Value),
                        new MySqlParameter("@MothersName", student.MothersName ?? (object)DBNull.Value),
                        new MySqlParameter("@GuardianName", student.GuardianName ?? (object)DBNull.Value),
                        new MySqlParameter("@GuardianContact", student.GuardianContact ?? (object)DBNull.Value),
                        new MySqlParameter("@ContactPerson", student.ContactPerson ?? (object)DBNull.Value)
                    });

                    await studentCommand.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    return new ApiResponse<int>
                    {
                        Success = true,
                        Message = "Student added successfully",
                        Data = Convert.ToInt32(insertedUserId)
                    };
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = "Failed to add student",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> UpdateStudentAsync(Studentclass student)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnections.GetConnection());
                var query = @"
                    UPDATE students 
                    SET StudentName = @StudentName, Gender = @Gender, GradeLevel = @GradeLevel, 
                        Section = @Section, Strand = @Strand, SchoolYear = @SchoolYear,
                        ParentName = @ParentName, ParentContact = @ParentContact,
                        FathersName = @FathersName, MothersName = @MothersName, 
                        GuardianName = @GuardianName, GuardianContact = @GuardianContact, 
                        ContactPerson = @ContactPerson,
                        TeacherID = @TeacherID, IsActive = @IsActive
                    WHERE StudentID = @StudentID";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(new[]
                {
                    new MySqlParameter("@StudentName", student.StudentName.ToUpper()),
                    new MySqlParameter("@Gender", student.Gender),
                    new MySqlParameter("@GradeLevel", student.GradeLevel ?? (object)DBNull.Value),
                    new MySqlParameter("@Section", student.Section ?? (object)DBNull.Value),
                    new MySqlParameter("@Strand", student.Strand ?? (object)DBNull.Value),
                    new MySqlParameter("@SchoolYear", student.SchoolYear ?? (object)DBNull.Value),
                    new MySqlParameter("@ParentName", student.ParentName?.ToUpper() ?? (object)DBNull.Value),
                    new MySqlParameter("@ParentContact", student.ParentContact ?? (object)DBNull.Value),
                    new MySqlParameter("@FathersName", student.FathersName?.ToUpper() ?? (object)DBNull.Value),
                    new MySqlParameter("@MothersName", student.MothersName?.ToUpper() ?? (object)DBNull.Value),
                    new MySqlParameter("@GuardianName", student.GuardianName?.ToUpper() ?? (object)DBNull.Value),
                    new MySqlParameter("@GuardianContact", student.GuardianContact ?? (object)DBNull.Value),
                    new MySqlParameter("@ContactPerson", student.ContactPerson ?? (object)DBNull.Value),
                    new MySqlParameter("@TeacherID", student.TeacherID ?? (object)DBNull.Value),
                    new MySqlParameter("@IsActive", student.IsActive),
                    new MySqlParameter("@StudentID", student.StudentID)
                });

                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();

                return new ApiResponse<bool>
                {
                    Success = rowsAffected > 0,
                    Message = rowsAffected > 0 ? "Student updated successfully" : "Student not found",
                    Data = rowsAffected > 0
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update student",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteStudentAsync(int studentId)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnections.GetConnection());
                var query = "UPDATE students SET IsActive = 0 WHERE StudentID = @StudentID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.Add(new MySqlParameter("@StudentID", studentId));

                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();

                return new ApiResponse<bool>
                {
                    Success = rowsAffected > 0,
                    Message = rowsAffected > 0 ? "Student deleted successfully" : "Student not found",
                    Data = rowsAffected > 0
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to delete student",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<Teacherclass>> GetTeacherByUserIdAsync(int userId)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnections.GetConnection());
                var query = @"
                    SELECT t.*, u.Username 
                    FROM teachers t 
                    INNER JOIN users u ON t.UserID = u.UserID 
                    WHERE t.UserID = @UserID AND t.IsActive = 1";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.Add(new MySqlParameter("@UserID", userId));
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var teacher = new Teacherclass
                    {
                        TeacherID = reader.GetInt32("TeacherID"),
                        UserID = reader.GetInt32("UserID"),
                        TeacherName = reader.GetString("TeacherName"),
                        Email = reader.GetString("Email"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        Position = reader.GetString("Position"),
                        Gender = reader.IsDBNull("Gender") ? null : reader.GetString("Gender"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        School_ID = reader.IsDBNull("School_ID") ? null : reader.GetString("School_ID"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel"),
                        Section = reader.IsDBNull("Section") ? null : reader.GetString("Section"),
                        Strand = reader.IsDBNull("Strand") ? null : reader.GetString("Strand"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DateRegister = reader.GetDateTime("DateRegister")
                    };
                    
                    return new ApiResponse<Teacherclass>
                    {
                        Success = true,
                        Message = "Teacher retrieved successfully",
                        Data = teacher
                    };
                }
                else
                {
                    return new ApiResponse<Teacherclass>
                    {
                        Success = false,
                        Message = "Teacher not found",
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<Teacherclass>
                {
                    Success = false,
                    Message = $"Error retrieving teacher: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<Teacherclass>> GetTeacherByIdAsync(int teacherId)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnections.GetConnection());
                var query = @"
                    SELECT t.*, u.Username 
                    FROM teachers t 
                    INNER JOIN users u ON t.UserID = u.UserID 
                    WHERE t.TeacherID = @TeacherID AND t.IsActive = 1";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.Add(new MySqlParameter("@TeacherID", teacherId));
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var teacher = new Teacherclass
                    {
                        TeacherID = reader.GetInt32("TeacherID"),
                        UserID = reader.GetInt32("UserID"),
                        TeacherName = reader.GetString("TeacherName"),
                        Email = reader.GetString("Email"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        Position = reader.GetString("Position"),
                        Gender = reader.IsDBNull("Gender") ? null : reader.GetString("Gender"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        School_ID = reader.IsDBNull("School_ID") ? null : reader.GetString("School_ID"),
                        GradeLevel = reader.IsDBNull("GradeLevel") ? null : reader.GetString("GradeLevel"),
                        Section = reader.IsDBNull("Section") ? null : reader.GetString("Section"),
                        Strand = reader.IsDBNull("Strand") ? null : reader.GetString("Strand"),
                        IsActive = reader.GetBoolean("IsActive"),
                        DateRegister = reader.GetDateTime("DateRegister")
                    };
                    
                    return new ApiResponse<Teacherclass>
                    {
                        Success = true,
                        Message = "Teacher retrieved successfully",
                        Data = teacher
                    };
                }
                else
                {
                    return new ApiResponse<Teacherclass>
                    {
                        Success = false,
                        Message = "Teacher not found",
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<Teacherclass>
                {
                    Success = false,
                    Message = "Failed to retrieve teacher",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        private string GenerateUsername(string studentName)
        {
            // Generate username as firstname + current year
            var nameParts = studentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts[0].ToLower();
            var currentYear = DateTime.Now.Year;
            return $"{firstName}{currentYear}";
        }

        private string GeneratePassword()
        {
            // Generate password as "stud" + 4 random numbers
            var random = new Random().Next(1000, 9999);
            return $"stud{random}";
        }
    }
}