using Microsoft.AspNetCore.Mvc;
using SharedProject;
using System.Text.Json;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IncidentReportController : ControllerBase
    {
        private readonly ILogger<IncidentReportController> _logger;
        private readonly IncidentReportService _incidentReportService;

        public IncidentReportController(ILogger<IncidentReportController> logger, IncidentReportService incidentReportService)
        {   
            _logger = logger;
            _incidentReportService = incidentReportService;
        }

        [HttpGet("daily-submission-count/{complainantName}")]
        public async Task<IActionResult> GetDailySubmissionCount(string complainantName)
        {
            try
            {
                var count = await _incidentReportService.GetDailySubmissionCountAsync(complainantName, DateTime.Now);
                
                return Ok(new
                {
                    success = true,
                    count = count,
                    date = DateTime.Now.Date
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily submission count");
                return StatusCode(500, new { success = false, message = "Error retrieving daily submission count" });
            }
        }

        [HttpPost("simplified")]
        [RequestSizeLimit(52428800)] // 50MB limit
        public async Task<IActionResult> CreateSimplifiedIncidentReport([FromBody] SimplifiedIncidentReportRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new simplified incident report for: {FullName}", request.FullName);
                
                // Log photo size if present
                if (!string.IsNullOrEmpty(request.EvidencePhotoBase64))
                {
                    var photoSize = System.Text.Encoding.UTF8.GetByteCount(request.EvidencePhotoBase64);
                    _logger.LogInformation("Photo size: {PhotoSize} bytes ({PhotoSizeMB} MB)", photoSize, photoSize / 1024.0 / 1024.0);
                }

                var result = await _incidentReportService.CreateSimplifiedIncidentReportAsync(request);

                var response = new
                {
                    success = true,
                    message = "Incident report submitted successfully",
                    incidentID = result.IncidentId,
                    referenceNumber = result.ReferenceNumber,
                    dateSubmitted = DateTime.Now
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating simplified incident report: {Message}\nStack Trace: {StackTrace}", 
                    ex.Message, ex.StackTrace);
                return StatusCode(500, new { Success = false, Message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateIncidentReport([FromBody] IncidentReportRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new incident report for complainant: {ComplainantName}", request.ComplainantName);

                // REMOVED: Daily submission limit check - POD officers need to submit multiple reports during rounds
                // var dailyCount = await _incidentReportService.GetDailySubmissionCountAsync(request.ComplainantName, DateTime.Now);
                // if (dailyCount >= 2)
                // {
                //     return BadRequest(new
                //     {
                //         success = false,
                //         message = "You have reached the daily limit of 2 incident reports. Please try again tomorrow."
                //     });
                // }

                var result = await _incidentReportService.CreateIncidentReportAsync(request);

                var response = new
                {
                    success = true,  
                    message = "Incident report submitted successfully",
                    incidentID = result.IncidentId,
                    referenceNumber = result.ReferenceNumber,
                    dateSubmitted = DateTime.Now
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating incident report");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetIncidentReports(
            [FromQuery] string? status = null, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string? schoolName = null,
            [FromQuery] string? division = null,
            [FromQuery] string? region = null,
            [FromQuery] string? district = null,
            [FromQuery] string? userLocation = null,
            [FromQuery] DateTime? startDate = null)
        {
            try
            {
                _logger.LogInformation("Retrieving incident reports with status: {Status}, startDate: {StartDate}, page: {Page}, location filters: School={School}, Division={Division}, Region={Region}, District={District}, UserLocation={UserLocation}", 
                    status, startDate, page, schoolName, division, region, district, userLocation);

                var reports = await _incidentReportService.GetIncidentReportsAsync(status, page, pageSize, schoolName, division, region, district, userLocation, startDate);

                var response = new
                {
                    success = true,
                    data = reports,
                    totalCount = reports.Count,
                    page = page,
                    pageSize = pageSize
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incident reports");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("teacher/{teacherId}")]
        public async Task<IActionResult> GetIncidentReportsByTeacher(int teacherId, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting incident reports for teacher ID: {TeacherId}, status: {Status}, page: {Page}, pageSize: {PageSize}", teacherId, status, page, pageSize);
                var reports = await _incidentReportService.GetIncidentReportsByTeacherAsync(teacherId, status, page, pageSize);
                
                _logger.LogInformation("Returning {Count} incident reports for teacher {TeacherId}", reports.Count, teacherId);
                
                return Ok(new
                {
                    success = true,
                    data = reports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident reports for teacher {TeacherId}: {Message}\nStack trace: {StackTrace}", teacherId, ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving incident reports: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIncidentReport(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving incident report with ID: {IncidentID}", id);

                // Here you would typically query the database by ID
                await Task.Delay(500); // Simulate database operation

                var sampleReport = new
                {
                    IncidentID = id,
                    ComplainantName = "Juan Dela Cruz",
                    ComplainantGrade = "Grade 12",
                    ComplainantStrand = "STEM",
                    ComplainantSection = "12-STEM A",
                    VictimName = "Ana Reyes",
                    RoomNumber = "Room 201",
                    VictimContact = "09123456789",
                    IncidentDescription = "Bullying incident during lunch break. The respondent was verbally harassing the victim.",
                    RespondentName = "Pedro Santos",
                    AdviserName = "Maria Santos",
                    PODIncharge = "John Doe",
                    EvidencePhotoPath = "evidence_photo_1.jpg",
                    DateReported = DateTime.Now.AddDays(-1),
                    Status = "Pending",
                    DateResolved = (DateTime?)null
                };

                return Ok(new { Success = true, Data = sampleReport });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incident report with ID: {IncidentID}", id);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateIncidentReportStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                _logger.LogInformation("Updating incident report {IncidentID} status to {Status}", id, request.Status);

                var success = await _incidentReportService.UpdateIncidentReportStatusAsync(id, request.Status);

                if (success)
                {
                    var response = new
                    {
                        Success = true,
                        Message = $"Incident report status updated to {request.Status}",
                        IncidentID = id,
                        NewStatus = request.Status,
                        UpdatedAt = DateTime.Now
                    };

                    return Ok(response);
                }
                else
                {
                    return NotFound(new { Success = false, Message = "Incident report not found or is inactive" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating incident report {IncidentID} status", id);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        //[HttpPost("{id}/comments")]
        //public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Adding comment to incident report {IncidentID}", id);

        //        // Here you would typically save the comment to the database
        //        await Task.Delay(500); // Simulate database operation

        //        var response = new
        //        {
        //            Success = true,
        //            Message = "Comment added successfully",
        //            IncidentID = id,
        //            CommentID = new Random().Next(1000, 9999),
        //            DateAdded = DateTime.Now
        //        };

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error adding comment to incident report {IncidentID}", id);
        //        return StatusCode(500, new { Success = false, Message = "Internal server error" });
        //    }
        //}

        [HttpGet("pod-teachers")]
        public async Task<IActionResult> GetPODTeachers()
        {
            try
            {
                _logger.LogInformation("Retrieving POD teachers");

                var podTeachers = await _incidentReportService.GetPODTeachersAsync();

                _logger.LogInformation("Found {Count} POD teachers in database", podTeachers.Count);

                var response = new
                {
                    Success = true,
                    Data = podTeachers
                };

                _logger.LogInformation("Returning {Count} POD teachers", podTeachers.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving POD teachers");
                return StatusCode(500, new { Success = false, Message = "Error retrieving POD teachers" });
            }
        }

        [HttpGet("student-info/{username}")]
        public async Task<IActionResult> GetStudentInfo(string username)
        {
            try
            {
                _logger.LogInformation("Retrieving student information for user: {Username}", username);

                var studentInfo = await _incidentReportService.GetStudentInfoByUsernameAsync(username);

                if (studentInfo == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Student information not found"
                    });
                }

                var response = new
                {
                    Success = true,
                    Data = new
                    {
                        StudentName = studentInfo.StudentName,
                        GradeLevel = studentInfo.GradeLevel,
                        Section = studentInfo.Section,
                        Strand = studentInfo.Strand,
                        TrackStrand = studentInfo.Strand,
                        SchoolName = studentInfo.SchoolName ?? string.Empty,
                        SchoolID = studentInfo.School_ID
                    }
                };

                _logger.LogInformation("Returning student info for {Username}: {StudentName}", username, studentInfo.StudentName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student information for user {Username}", username);
                return StatusCode(500, new { Success = false, Message = "Error retrieving student information" });
            }
        }

        [HttpGet("pod-location/{username}")]
        public async Task<IActionResult> GetPODLocation(string username)
        {
            try
            {
                _logger.LogInformation("Retrieving POD location for user: {Username}", username);

                var location = await _incidentReportService.GetPODLocationAsync(username);

                var response = new
                {
                    success = true,
                    data = new
                    {
                        schoolName = location.SchoolName,
                        division = location.Division,
                        region = location.Region,
                        district = location.District
                    }
                };

                _logger.LogInformation("Returning POD location for {Username}: School={SchoolName}, Division={Division}", 
                    username, location.SchoolName, location.Division);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving POD location for user {Username}", username);
                return StatusCode(500, new { Success = false, Message = "Error retrieving POD location" });
            }
        }

        [HttpGet("teachers-by-location")]
        public async Task<IActionResult> GetTeachersByLocation(
            [FromQuery] string? schoolName = null,
            [FromQuery] string? division = null,
            [FromQuery] string? region = null,
            [FromQuery] string? district = null,
            [FromQuery] string? position = null)
        {
            try
            {
                _logger.LogInformation("Retrieving teachers by location - School: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}, Position: {Position}", 
                    schoolName, division, region, district, position);

                var teachers = await _incidentReportService.GetTeachersByLocationAsync(schoolName, division, region, district, position);

                var response = new
                {
                    Success = true,
                    Data = teachers
                };

                _logger.LogInformation("Returning {Count} teachers filtered by location", teachers.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving teachers by location");
                return StatusCode(500, new { Success = false, Message = "Error retrieving teachers by location" });
            }
        }

        [HttpGet("by-complainant/{complainantName}")]
        public async Task<IActionResult> GetIncidentReportsByComplainant(string complainantName)
        {
            try
            {
                _logger.LogInformation("Getting incident reports for complainant: {ComplainantName}", complainantName);
                
                var reports = await _incidentReportService.GetIncidentReportsByComplainantAsync(complainantName);

                return Ok(new
                {
                    success = true,
                    data = reports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident reports for complainant {ComplainantName}", complainantName);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("by-respondent/{respondentName}")]
        public async Task<IActionResult> GetIncidentReportsByRespondent(string respondentName)
        {
            try
            {
                _logger.LogInformation("Getting incident reports for respondent: {RespondentName}", respondentName);
                
                var reports = await _incidentReportService.GetIncidentReportsByRespondentAsync(respondentName);

                return Ok(new
                {
                    success = true,
                    data = reports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident reports for respondent {RespondentName}", respondentName);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("students-by-location")]
        public async Task<IActionResult> GetStudentsByLocation(
            [FromQuery] string? schoolName = null,
            [FromQuery] string? division = null,
            [FromQuery] string? region = null,
            [FromQuery] string? district = null)
        {
            try
            {
                _logger.LogInformation("Retrieving students by location - School: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}", 
                    schoolName, division, region, district);

                var students = await _incidentReportService.GetStudentsByLocationAsync(schoolName, division, region, district);

                var response = new
                {
                    Success = true,
                    Data = students
                };

                _logger.LogInformation("Returning {Count} students filtered by location", students.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students by location");
                return StatusCode(500, new { Success = false, Message = "Error retrieving students by location" });
            }
        }


        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetIncidentReportById(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving incident report with ID: {IncidentID}", id);

                var incidentReport = await _incidentReportService.GetIncidentReportByIdAsync(id);

                if (incidentReport == null)
                {
                    return NotFound(new { Success = false, Message = "Incident report not found" });
                }

                var response = new
                {
                    Success = true,
                    Data = incidentReport
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incident report {IncidentID}", id);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("category-trend")]
        public async Task<IActionResult> GetCategoryTrend(
            [FromQuery] string? schoolName = null,
            [FromQuery] string? division = null,
            [FromQuery] string? region = null,
            [FromQuery] string? district = null)
        {
            try
            {
                _logger.LogInformation("=== API CALL: GetCategoryTrend ===");
                _logger.LogInformation("Request parameters - School: {SchoolName}, Division: {Division}, Region: {Region}, District: {District}", 
                    schoolName ?? "NULL", division ?? "NULL", region ?? "NULL", district ?? "NULL");

                var trendData = await _incidentReportService.GetCategoryTrendDataAsync(schoolName, division, region, district);

                _logger.LogInformation("API Response - Returning {Count} months of trend data", trendData.Count);
                
                if (trendData.Any())
                {
                    var summary = $"Minor: {trendData.Sum(t => t.MinorCount)}, Major: {trendData.Sum(t => t.MajorCount)}, Prohibited: {trendData.Sum(t => t.ProhibitedCount)}";
                    _logger.LogInformation("Data summary: {Summary}", summary);
                }
                else
                {
                    _logger.LogWarning("WARNING: No trend data returned from service");
                }

                var response = new
                {
                    Success = true,
                    Data = trendData
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL ERROR in GetCategoryTrend API: {Message}\nStack Trace: {StackTrace}", 
                    ex.Message, ex.StackTrace);
                return StatusCode(500, new { Success = false, Message = $"Error retrieving category trend data: {ex.Message}" });
            }
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats(
            [FromQuery] string? schoolName = null,
            [FromQuery] string? division = null,
            [FromQuery] string? region = null,
            [FromQuery] string? district = null)
        {
            try
            {
                var stats = await _incidentReportService.GetDashboardStatsAsync(schoolName, division, region, district);
                return Ok(new { Success = true, Data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpPost("pod-login")]
        public async Task<IActionResult> PODLogin([FromBody] PODLoginRequest request)
        {
            try
            {
                _logger.LogInformation("POD login attempt for username: {Username}", request.Username);

                var podInfo = await _incidentReportService.AuthenticatePODAsync(request.Username, request.Password);

                if (podInfo == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid credentials or user is not a POD"
                    });
                }

                var response = new
                {
                    success = true,
                    message = "Login successful",
                    data = new
                    {
                        teacherName = podInfo.TeacherName,
                        schoolName = podInfo.SchoolName,
                        schoolID = podInfo.SchoolID,
                        position = podInfo.Position,
                        division = podInfo.Division,
                        region = podInfo.Region,
                        district = podInfo.District
                    }
                };

                _logger.LogInformation("POD login successful for {Username} - School: {SchoolName}", request.Username, podInfo.SchoolName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POD login for username {Username}", request.Username);
                return StatusCode(500, new { success = false, message = "Error during login" });
            }
        }

        [HttpGet("school-students/{schoolName}")]
        public async Task<IActionResult> GetSchoolStudents(string schoolName)
        {
            try
            {
                _logger.LogInformation("Retrieving students for school: {SchoolName}", schoolName);

                var students = await _incidentReportService.GetStudentsBySchoolAsync(schoolName);

                var response = new
                {
                    success = true,
                    data = students
                };

                _logger.LogInformation("Returning {Count} students from {SchoolName}", students.Count, schoolName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students for school {SchoolName}", schoolName);
                return StatusCode(500, new { success = false, message = "Error retrieving students" });
            }
        }

        [HttpGet("reference/{referenceNumber}")]
        public async Task<IActionResult> GetIncidentReportByReference(string referenceNumber)
        {
            try
            {
                _logger.LogInformation("Retrieving incident report with reference number: {ReferenceNumber}", referenceNumber);

                var incidentReport = await _incidentReportService.GetIncidentReportByReferenceNumberAsync(referenceNumber);

                if (incidentReport == null)
                {
                    return NotFound(new { Success = false, Message = "Incident report not found" });
                }

                return Ok(new
                {
                    Success = true,
                    Data = incidentReport
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incident report with reference number: {ReferenceNumber}", referenceNumber);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpPost("official")]
        public async Task<IActionResult> CreateOfficialIncidentReport([FromBody] OfficialIncidentReportRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new official incident report for ReporterID: {ReporterID}", request.ReporterID);
                var result = await _incidentReportService.CreateOfficialIncidentReportAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating official incident report");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("official")]
        public async Task<IActionResult> GetOfficialIncidentReports([FromQuery] int? reporterId = null, [FromQuery] string? schoolName = null)
        {
            try
            {
                var reports = await _incidentReportService.GetOfficialIncidentReportsAsync(reporterId, schoolName);
                return Ok(new { success = true, data = reports });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting official incident reports");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("official/summary")]
        public async Task<IActionResult> GetOfficialIncidentReportSummary([FromQuery] string? schoolName = null)
        {
            try
            {
                var summary = await _incidentReportService.GetOfficialIncidentReportSummaryAsync(schoolName);
                return Ok(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting official incident report summary");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("official/reporter/{reporterId}")]
        public async Task<IActionResult> GetOfficialIncidentReportsByReporter(int reporterId)
        {
            try
            {
                var reports = await _incidentReportService.GetOfficialIncidentReportsByReporterAsync(reporterId);
                return Ok(new { success = true, data = reports });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting official incident reports for reporter {reporterId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("student-adviser/{studentName}")]
        public async Task<IActionResult> GetStudentAdviser(string studentName, [FromQuery] string schoolName)
        {
            try
            {
                var adviserName = await _incidentReportService.GetStudentAdviserByNameAsync(studentName, schoolName);
                if (adviserName == null)
                    return Ok(new { success = true, data = new { adviserName = "" } });

                return Ok(new { success = true, data = new { adviserName } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("violation-types")]
        public async Task<IActionResult> GetViolationTypes()
        {
            try
            {
                var violations = await _incidentReportService.GetViolationTypesAsync();
                return Ok(new { success = true, data = violations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving violation types");
                return StatusCode(500, new { success = false, message = "Error retrieving violation types" });
            }
        }

        [HttpGet("student/{studentName}")]
        public async Task<IActionResult> GetStudentCases(string studentName)
        {
            try
            {
                var cases = await _incidentReportService.GetStudentCasesAsync(studentName);
                return Ok(new { success = true, data = cases });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cases for student {StudentName}", studentName);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("students-with-cases/adviser/{adviserName}")]
        public async Task<IActionResult> GetStudentsWithCasesByAdviser(string adviserName)
        {
            try
            {
                _logger.LogInformation("API: Getting students with cases for adviser: {AdviserName}", adviserName);
                var students = await _incidentReportService.GetStudentsWithCasesByAdviserAsync(adviserName);
                return Ok(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students with cases for adviser {AdviserName}", adviserName);
                return StatusCode(500, new { success = false, message = "Error retrieving students with cases" });
            }
        }

        /// <summary>Students with cases + full violation list in one call from SimplifiedIncidentReports — same source as "3 Minor" count.</summary>
        [HttpGet("students-with-cases-details/adviser/{adviserName}")]
        public async Task<IActionResult> GetStudentsWithCasesAndDetailsByAdviser(string adviserName)
        {
            try
            {
                _logger.LogInformation("API: Getting students with cases and details for adviser: {AdviserName}", adviserName);
                var students = await _incidentReportService.GetStudentsWithCasesAndDetailsByAdviserAsync(adviserName);
                return Ok(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students with case details for adviser {AdviserName}", adviserName);
                return StatusCode(500, new { success = false, message = "Error retrieving students with case details" });
            }
        }
    }

    public class PODLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? ChangeReason { get; set; }
    }

    //public class AddCommentRequest
    //{
    //    public string CommentText { get; set; } = string.Empty;
    //    public bool IsInternal { get; set; } = false;
    //    [HttpPut("{id}/send-to-guidance")]
    //    public async Task<IActionResult> SendToGuidance(int id, [FromQuery] string referredBy)
    //    {
    //        try
    //        {
    //            var result = await _incidentReportService.SendToGuidanceAsync(id, referredBy);
    //            if (result > 0)
    //                return Ok(new { success = true, message = "Incident report sent to Guidance Counselor." });
                
    //            return NotFound(new { success = false, message = "Incident report not found." });
    //        }
    //        catch (Exception ex)
    //        {
    //            return StatusCode(500, new { success = false, message = ex.Message });
    //        }
    //    }
}
