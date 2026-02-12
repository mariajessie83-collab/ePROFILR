using Microsoft.AspNetCore.Mvc;
using SharedProject;
using Server.Services;
using Server.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TeacherService _teacherService;
        private readonly Dbconnections _dbConnections;
        private readonly ILogger<AuthController> _logger;

        public AuthController(TeacherService teacherService, Dbconnections dbConnections, ILogger<AuthController> logger)
        {
            _teacherService = teacherService;
            _dbConnections = dbConnections;
            _logger = logger;
        }

        [HttpPost("register/teacher")]
        public async Task<ActionResult<ApiResponse<int>>> RegisterTeacher([FromBody] TeacherRegistrationRequest request)
        {
            try
            {
                // Log the incoming request for debugging
                Console.WriteLine($"Teacher registration request received: {System.Text.Json.JsonSerializer.Serialize(request)}");
                Console.WriteLine($"Request details:");
                Console.WriteLine($"- TeacherName: {request?.TeacherName ?? "NULL"}");
                Console.WriteLine($"- Email: {request?.Email ?? "NULL"}");
                Console.WriteLine($"- Username: {request?.Username ?? "NULL"}");
                Console.WriteLine($"- Position: {request?.Position ?? "NULL"}");
                Console.WriteLine($"- PhoneNumber: {request?.PhoneNumber ?? "NULL"}");

                if (request == null)
                {
                    return Ok(new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Request body is null",
                        Errors = new List<string> { "No data received" }
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    Console.WriteLine($"Model validation errors: {string.Join(", ", errors)}");

                    return Ok(new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var result = await _teacherService.RegisterTeacherAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    Console.WriteLine($"Teacher service error: {result.Message}");
                    return Ok(result); // Return Ok with error message instead of BadRequest
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in RegisterTeacher: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpPost("register/student")]
        public async Task<ActionResult<ApiResponse<int>>> RegisterStudent([FromBody] StudentRegistrationRequest request)
        {
            try
            {
                // Log the incoming request for debugging
                Console.WriteLine($"Student registration request received: {System.Text.Json.JsonSerializer.Serialize(request)}");
                Console.WriteLine($"Request details:");
                Console.WriteLine($"- StudentName: {request?.StudentName ?? "NULL"}");
                Console.WriteLine($"- Username: {request?.Username ?? "NULL"}");
                Console.WriteLine($"- Gender: {request?.Gender ?? "NULL"}");
                Console.WriteLine($"- GradeLevel: {request?.GradeLevel ?? "NULL"}");
                Console.WriteLine($"- SchoolYear: {request?.SchoolYear ?? "NULL"}");
                Console.WriteLine($"- ParentName: {request?.ParentName ?? "NULL"}");
                Console.WriteLine($"- SchoolID: {request?.SchoolID?.ToString() ?? "NULL"}");
                Console.WriteLine($"- SchoolName: {request?.SchoolName ?? "NULL"}");
                Console.WriteLine($"- School_ID: {request?.School_ID ?? "NULL"}");

                if (request == null)
                {
                    return Ok(new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Request body is null",
                        Errors = new List<string> { "No data received" }
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    Console.WriteLine($"Model validation errors: {string.Join(", ", errors)}");

                    return Ok(new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var result = await _teacherService.RegisterStudentAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    Console.WriteLine($"Student service error: {result.Message}");
                    return Ok(result); // Return Ok with error message instead of BadRequest
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in RegisterStudent: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Username and password are required",
                        Errors = new List<string> { "Invalid credentials" }
                    });
                }

                // Validate credentials against database
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // First, check if user exists and get basic info
                var userQuery = @"
                    SELECT u.UserID, u.Username, u.UserRole, u.IsActive
                    FROM Users u
                    WHERE u.Username = @Username AND u.Password = @Password AND u.IsActive = 1";

                using var userCommand = new MySqlCommand(userQuery, connection);
                userCommand.Parameters.AddWithValue("@Username", request.Username);
                userCommand.Parameters.AddWithValue("@Password", request.Password);

                using var userReader = await userCommand.ExecuteReaderAsync();

                if (await userReader.ReadAsync())
                {
                    var userID = userReader.GetInt32("UserID");
                    var username = userReader.GetString("Username");
                    var userRole = userReader.GetString("UserRole");
                    var isActive = userReader.GetBoolean("IsActive");

                    userReader.Close();

                    object userInfo;

                    if (userRole.ToLower() == "teacher")
                    {
                        // Get teacher-specific information including TeacherID and School Details
                        var teacherQuery = @"
                            SELECT t.TeacherID, t.TeacherName, t.Position, s.SchoolName, s.Region, s.Division, s.District, s.School_ID
                            FROM Teachers t
                            LEFT JOIN Schools s ON t.SchoolID = s.SchoolID
                            WHERE t.UserID = @UserID AND t.IsActive = 1";

                        using var teacherCommand = new MySqlCommand(teacherQuery, connection);
                        teacherCommand.Parameters.AddWithValue("@UserID", userID);

                        using var teacherReader = await teacherCommand.ExecuteReaderAsync();

                        if (await teacherReader.ReadAsync())
                        {
                            userInfo = new
                            {
                                UserID = userID,
                                Username = username,
                                UserRole = userRole,
                                TeacherID = teacherReader.GetInt32("TeacherID"),
                                TeacherName = teacherReader.GetString("TeacherName"),
                                Position = teacherReader.GetString("Position"),
                                SchoolName = teacherReader.IsDBNull("SchoolName") ? "" : teacherReader.GetString("SchoolName"),
                                School_ID = teacherReader.IsDBNull("School_ID") ? "" : teacherReader.GetString("School_ID"),
                                Region = teacherReader.IsDBNull("Region") ? "" : teacherReader.GetString("Region"),
                                Division = teacherReader.IsDBNull("Division") ? "" : teacherReader.GetString("Division"),
                                District = teacherReader.IsDBNull("District") ? "" : teacherReader.GetString("District"),
                                IsActive = isActive
                            };

                            _logger.LogInformation("Teacher login successful for user: {Username}, TeacherID: {TeacherID}, Position: {Position}, School: {School}", 
                                username, teacherReader.GetInt32("TeacherID"), teacherReader.GetString("Position"), teacherReader.IsDBNull("SchoolName") ? "N/A" : teacherReader.GetString("SchoolName"));
                        }
                        else
                        {
                            return BadRequest(new ApiResponse<object>
                            {
                                Success = false,
                                Message = "Teacher account not found or inactive",
                                Errors = new List<string> { "Invalid credentials" }
                            });
                        }
                    }
                    else if (userRole.ToLower() == "student")
                    {
                        // Get student-specific information
                        var studentQuery = @"
                            SELECT StudentName, GradeLevel, Section
                            FROM Students
                            WHERE UserID = @UserID AND IsActive = 1";

                        using var studentCommand = new MySqlCommand(studentQuery, connection);
                        studentCommand.Parameters.AddWithValue("@UserID", userID);

                        using var studentReader = await studentCommand.ExecuteReaderAsync();

                        if (await studentReader.ReadAsync())
                        {
                            userInfo = new
                            {
                                UserID = userID,
                                Username = username,
                                UserRole = userRole,
                                StudentName = studentReader.GetString("StudentName"),
                                GradeLevel = studentReader.GetString("GradeLevel"),
                                Section = studentReader.IsDBNull("Section") ? string.Empty : studentReader.GetString("Section"),
                                IsActive = isActive
                            };

                            _logger.LogInformation("Student login successful for user: {Username}, Grade: {GradeLevel}", 
                                username, studentReader.GetString("GradeLevel"));
                        }
                        else
                        {
                            return BadRequest(new ApiResponse<object>
                            {
                                Success = false,
                                Message = "Student account not found or inactive",
                                Errors = new List<string> { "Invalid credentials" }
                            });
                        }
                    }
                    else if (userRole.ToLower() == "admin" || userRole.ToLower() == "schoolhead" || userRole.ToLower() == "division")
                    {
                        // Get admin account information
                        var adminQuery = @"
                            SELECT AdminAccountID, AccountType, FullName, SchoolName, Division, Region, District, DivisionName
                            FROM AdminAccounts
                            WHERE UserID = @UserID AND IsActive = 1";

                        using var adminCommand = new MySqlCommand(adminQuery, connection);
                        adminCommand.Parameters.AddWithValue("@UserID", userID);

                        using var adminReader = await adminCommand.ExecuteReaderAsync();

                        if (await adminReader.ReadAsync())
                        {
                            userInfo = new
                            {
                                UserID = userID,
                                Username = username,
                                UserRole = userRole,
                                AdminAccountID = adminReader.GetInt32("AdminAccountID"),
                                AccountType = adminReader.GetString("AccountType"),
                                FullName = adminReader.GetString("FullName"),
                                SchoolName = adminReader.IsDBNull("SchoolName") ? null : adminReader.GetString("SchoolName"),
                                Division = adminReader.IsDBNull("Division") ? null : adminReader.GetString("Division"),
                                Region = adminReader.IsDBNull("Region") ? null : adminReader.GetString("Region"),
                                District = adminReader.IsDBNull("District") ? null : adminReader.GetString("District"),
                                DivisionName = adminReader.IsDBNull("DivisionName") ? null : adminReader.GetString("DivisionName"),
                                IsActive = isActive
                            };

                            _logger.LogInformation("{AccountType} login successful for user: {Username}, AdminAccountID: {AdminAccountID}",
                                adminReader.GetString("AccountType"), username, adminReader.GetInt32("AdminAccountID"));
                        }
                        else
                        {
                            return BadRequest(new ApiResponse<object>
                            {
                                Success = false,
                                Message = $"{userRole} account not found or inactive",
                                Errors = new List<string> { "Invalid credentials" }
                            });
                        }
                    }
                    else
                    {
                        // Unknown user role
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Invalid user role",
                            Errors = new List<string> { "Invalid credentials" }
                        });
                    }

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Login successful",
                        Data = userInfo
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid username or password",
                        Errors = new List<string> { "Invalid credentials" }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Login failed",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("health")]
        public ActionResult<ApiResponse<string>> Health()
        {
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Auth API is running",
                Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        // New endpoint for schools dropdown data
        [HttpGet("schools")]
        public async Task<ActionResult<ApiResponse<List<School>>>> GetSchools()
        {
            try
            {
                var result = await _teacherService.GetSchoolsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<School>>
                {
                    Success = false,
                    Message = "Failed to retrieve schools",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // Student management endpoints for advisers
        [HttpGet("students/teacher/{teacherId}")]
        public async Task<ActionResult<ApiResponse<List<Studentclass>>>> GetStudentsByTeacher(int teacherId)
        {
            try
            {
                var result = await _teacherService.GetStudentsByTeacherAsync(teacherId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<Studentclass>>
                {
                    Success = false,
                    Message = "Failed to retrieve students",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("students")]
        public async Task<ActionResult<ApiResponse<int>>> AddStudent([FromBody] Studentclass student)
        {
            try
            {
                var result = await _teacherService.AddStudentAsync(student);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "Failed to add student",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("students/{studentId}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateStudent(int studentId, [FromBody] Studentclass student)
        {
            try
            {
                student.StudentID = studentId;
                var result = await _teacherService.UpdateStudentAsync(student);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update student",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("students/{studentId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteStudent(int studentId)
        {
            try
            {
                var result = await _teacherService.DeleteStudentAsync(studentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to delete student",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("teachers/{teacherId}")]
        public async Task<ActionResult<ApiResponse<Teacherclass>>> GetTeacher(int teacherId)
        {
            try
            {
                var result = await _teacherService.GetTeacherByIdAsync(teacherId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<Teacherclass>
                {
                    Success = false,
                    Message = "Failed to retrieve teacher",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("teachers/by-user/{userId}")]
        public async Task<ActionResult<ApiResponse<Teacherclass>>> GetTeacherByUserId(int userId)
        {
            try
            {
                var result = await _teacherService.GetTeacherByUserIdAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<Teacherclass>
                {
                    Success = false,
                    Message = "Failed to retrieve teacher",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
