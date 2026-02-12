using Microsoft.AspNetCore.Mvc;
using Server.Services;
using SharedProject;
using System.Text.Json;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentProfileCaseRecordController : ControllerBase
    {
        private readonly StudentProfileCaseRecordService _service;
        private readonly ParentConferencePdfService _pdfService;
        private readonly StudentCaseRecordPdfService _caseRecordPdfService;
        private readonly YakapPdfService _yakapPdfService;
        private readonly AnnexBPdfService _annexBPdfService;
        private readonly AnnexAPdfService _annexAPdfService;
        private readonly ILogger<StudentProfileCaseRecordController> _logger;

        public StudentProfileCaseRecordController(
            StudentProfileCaseRecordService service, 
            ParentConferencePdfService pdfService, 
            StudentCaseRecordPdfService caseRecordPdfService,
            YakapPdfService yakapPdfService,
            AnnexBPdfService annexBPdfService,
            AnnexAPdfService annexAPdfService,
            ILogger<StudentProfileCaseRecordController> logger)
        {
            _service = service;
            _pdfService = pdfService;
            _caseRecordPdfService = caseRecordPdfService;
            _yakapPdfService = yakapPdfService;
            _annexBPdfService = annexBPdfService;
            _annexAPdfService = annexAPdfService;
            _logger = logger;
        }

        [HttpGet("violation-types")]
        public async Task<IActionResult> GetViolationTypes([FromQuery] string? category = null)
        {
            try
            {
                _logger.LogInformation("Getting violation types for category: {Category}", category);
                var violations = await _service.GetViolationTypesAsync(category);
                
                return Ok(new
                {
                    success = true,
                    data = violations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting violation types");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving violation types"
                });
            }
        }

        [HttpGet("violation-category")]
        public async Task<IActionResult> GetViolationCategory([FromQuery] string violationName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(violationName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Violation name is required"
                    });
                }

                _logger.LogInformation("Getting violation category for violation: {ViolationName}", violationName);
                var category = await _service.GetViolationCategoryAsync(violationName);
                
                if (!string.IsNullOrEmpty(category))
                {
                    return Ok(new
                    {
                        success = true,
                        data = category
                    });
                }
                else
                {
                    _logger.LogWarning("Category not found for violation: {ViolationName}", violationName);
                    return Ok(new
                    {
                        success = false,
                        message = $"Category not found for violation: {violationName}",
                        data = (string?)null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting violation category");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving violation category"
                });
            }
        }

        [HttpGet("search-teachers")]
        public async Task<IActionResult> SearchTeachers([FromQuery] string searchTerm, [FromQuery] string? schoolName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return Ok(new
                    {
                        success = true,
                        data = new List<TeacherSearchResult>()
                    });
                }

                _logger.LogInformation("Searching teachers with term: {SearchTerm}, school: {SchoolName}", searchTerm, schoolName);
                var teachers = await _service.SearchTeachersAsync(searchTerm, schoolName);
                
                return Ok(new
                {
                    success = true,
                    data = teachers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching teachers");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error searching teachers"
                });
            }
        }

        [HttpGet("student-adviser/{studentName}")]
        public async Task<IActionResult> GetStudentAdviser(string studentName, [FromQuery] string? schoolName = null)
        {
            if (string.IsNullOrWhiteSpace(studentName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Student name is required"
                });
            }

            try
            {
                _logger.LogInformation("=== CONTROLLER: GetStudentAdviser called ===");
                _logger.LogInformation("Getting adviser for student '{StudentName}' with school filter '{SchoolName}'", studentName, schoolName);
                var adviserInfo = await _service.GetStudentAdviserAsync(studentName, schoolName);
                _logger.LogInformation("Service returned {Count} results", adviserInfo.Count);
                
                // Log parent/guardian data for debugging
                if (adviserInfo.Any())
                {
                    var first = adviserInfo.First();
                    _logger.LogInformation("First result parent data: FathersName={FathersName}, MothersName={MothersName}, GuardianName={GuardianName}, GuardianContact={GuardianContact}, ContactPerson={ContactPerson}", 
                        first.FathersName, first.MothersName, first.GuardianName, first.GuardianContact, first.ContactPerson);
                    
                    // Log the JSON that will be sent
                    var json = JsonSerializer.Serialize(adviserInfo);
                    _logger.LogInformation("JSON Response: {Json}", json);
                }

                return Ok(new
                {
                    success = true,
                    data = adviserInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting adviser for student {StudentName}", studentName);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving adviser information",
                    error = ex.Message
                });
            }
        }

        [HttpGet("incident-student-info")]
        public async Task<IActionResult> GetIncidentStudentInfo([FromQuery] string studentName, [FromQuery] int? incidentId = null)
        {
            if (string.IsNullOrWhiteSpace(studentName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Student name is required"
                });
            }

            try
            {
                var info = await _service.GetIncidentReportStudentInfoAsync(studentName, incidentId);

                return Ok(new
                {
                    success = true,
                    data = info
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident student info for {StudentName}", studentName);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving incident student information"
                });
            }
        }

        [HttpGet("count-offenses")]
        public async Task<IActionResult> CountOffenses([FromQuery] string studentName)
        {
            if (string.IsNullOrWhiteSpace(studentName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Student name is required"
                });
            }

            try
            {
                _logger.LogInformation("Counting offenses for student: {StudentName}", studentName);
                var count = await _service.CountStudentOffensesAsync(studentName);
                
                return Ok(new
                {
                    success = true,
                    count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting offenses for {StudentName}", studentName);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error counting student offenses"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCaseRecords([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting case records with status: {Status}, page: {Page}, pageSize: {PageSize}", status, page, pageSize);
                var records = await _service.GetCaseRecordsAsync(status, page, pageSize);
                
                _logger.LogInformation("Returning {Count} case records", records.Count);
                
                return Ok(new
                {
                    success = true,
                    data = records
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records: {Message}\nStack trace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving case records: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("respondent/{studentName}")]
        public async Task<IActionResult> GetCaseRecordsByRespondent(string studentName)
        {
            try
            {
                _logger.LogInformation("Getting case records for respondent: {StudentName}", studentName);
                var records = await _service.GetCaseRecordsByStudentNameAsync(studentName);
                
                _logger.LogInformation("Returning {Count} case records for student {StudentName}", records.Count, studentName);
                
                return Ok(new
                {
                    success = true,
                    data = records
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records for student {StudentName}: {Message}\nStack trace: {StackTrace}", studentName, ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving case records: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetCaseRecordsByUsername(string username)
        {
            try
            {
                _logger.LogInformation("Getting case records for username: {Username}", username);
                var records = await _service.GetCaseRecordsByUsernameAsync(username);
                
                _logger.LogInformation("Returning {Count} case records for username {Username}", records.Count, username);
                
                return Ok(new
                {
                    success = true,
                    data = records
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records for username {Username}: {Message}\nStack trace: {StackTrace}", username, ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving case records: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("teacher/{teacherId}")]
        public async Task<IActionResult> GetCaseRecordsByTeacher(int teacherId, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting all case records for teacher ID: {TeacherId}, status: {Status}, page: {Page}, pageSize: {PageSize}", teacherId, status, page, pageSize);
                var records = await _service.GetCaseRecordsByTeacherAsync(teacherId, status, page, pageSize);
                
                _logger.LogInformation("Returning {Count} case records for teacher {TeacherId}", records.Count, teacherId);
                
                return Ok(new
                {
                    success = true,
                    data = records
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records for teacher {TeacherId}: {Message}\nStack trace: {StackTrace}", teacherId, ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving case records: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("teacher/{teacherId}/minor")]
        public async Task<IActionResult> GetMinorCaseRecordsByTeacher(int teacherId, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting minor case records for teacher ID: {TeacherId}, status: {Status}, page: {Page}, pageSize: {PageSize}", teacherId, status, page, pageSize);
                var records = await _service.GetMinorCaseRecordsByTeacherAsync(teacherId, status, page, pageSize);
                
                _logger.LogInformation("Returning {Count} minor case records for teacher {TeacherId}", records.Count, teacherId);
                
                return Ok(new
                {
                    success = true,
                    data = records
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting minor case records for teacher {TeacherId}: {Message}\nStack trace: {StackTrace}", teacherId, ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving minor case records: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("pod")]
        public async Task<IActionResult> GetCaseRecordsForPOD([FromQuery] int? schoolId = null, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                _logger.LogInformation("Getting case records for POD with routing logic. SchoolID: {SchoolID}, Status: {Status}, Page: {Page}, PageSize: {PageSize}", 
                    schoolId, status, page, pageSize);
                var records = await _service.GetCaseRecordsForPODAsync(schoolId, status, page, pageSize);
                
                _logger.LogInformation("Returning {Count} case records for POD (includes Major/Prohibited and students with 3+ minor cases)", records.Count);
                
                return Ok(new
                {
                    success = true,
                    data = records,
                    message = $"Returned {records.Count} case records (Major/Prohibited cases + students with 3+ minor cases)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case records for POD: {Message}\nStack trace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving case records for POD: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("{recordId}")]
        public async Task<IActionResult> GetCaseRecord(int recordId)
        {
            try
            {
                _logger.LogInformation("Getting case record with ID: {RecordId}", recordId);
                var record = await _service.GetCaseRecordByIdAsync(recordId);
                
                if (record == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Case record not found"
                    });
                }
                
                return Ok(new
                {
                    success = true,
                    data = record
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting case record");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving case record"
                });
            }
        }

        [HttpPut("{recordId}")]
        public async Task<IActionResult> UpdateCaseRecord(int recordId, [FromBody] StudentProfileCaseRecordModel request)
        {
            try
            {
                _logger.LogInformation("Updating case record {RecordId} for student: {StudentName}", recordId, request.StudentOffenderName);
                
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.StudentOffenderName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Student offender name is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.AdviserName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Adviser name is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.ViolationCommitted))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Violation committed is required"
                    });
                }

                var updated = await _service.UpdateCaseRecordAsync(recordId, request);
                
                if (updated)
                {
                    _logger.LogInformation("Successfully updated case record {RecordId}", recordId);
                    
                    return Ok(new
                    {
                        success = true,
                        message = "Case record updated successfully"
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Case record not found"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating case record {RecordId}: {Message}\nStack trace: {StackTrace}", recordId, ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error updating case record: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCaseRecord([FromBody] StudentProfileCaseRecordRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new case record for student: {StudentName}", request.StudentOffenderName);
                
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.StudentOffenderName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Student offender name is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.AdviserName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Adviser name is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.ViolationCommitted))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Violation committed is required"
                    });
                }

                // If "Other" violation is selected, ensure description is provided
                if (request.ViolationCommitted == "Other" && string.IsNullOrWhiteSpace(request.OtherViolationDescription))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Other violation description is required when 'Other' is selected"
                    });
                }

                // Validate that at least one of FathersName, MothersName, or GuardianName is provided
                if (string.IsNullOrWhiteSpace(request.FathersName) && 
                    string.IsNullOrWhiteSpace(request.MothersName) && 
                    string.IsNullOrWhiteSpace(request.GuardianName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "At least one of Father's Name, Mother's Name, or Guardian's Name is required"
                    });
                }

                var recordId = await _service.CreateCaseRecordAsync(request);
                
                _logger.LogInformation("Successfully created case record with ID: {RecordId}", recordId);
                
                return Ok(new
                {
                    success = true,
                    message = "Case record created successfully",
                    data = new { recordId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating case record: {Message}\nStack trace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error creating case record: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("incident-ids")]
        public async Task<IActionResult> GetIncidentIdsWithCaseRecords([FromQuery] string? schoolName = null)
        {
            try
            {
                var ids = await _service.GetIncidentIdsWithCaseRecordsAsync(schoolName);
                return Ok(new
                {
                    success = true,
                    data = ids
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident IDs with case records");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving incident IDs"
                });
            }
        }
        [HttpGet("incident-id/{incidentId}")]
        public async Task<IActionResult> GetCaseRecordByIncidentId(int incidentId)
        {
            try
            {
                var record = await _service.GetCaseRecordByIncidentIdAsync(incidentId);
                if (record == null)
                {
                    return NotFound(new { success = false, message = "Case record not found" });
                }
                return Ok(new { success = true, data = record });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error retrieving case record" });
            }
        }

        /// <summary>
        /// Get simplified case record by incident ID - uses SimplifiedStudentProfileCaseRecords table
        /// with all student profile data (sex, age, birthday, address, grade, section, adviser)
        /// </summary>
        [HttpGet("simplified-incident-id/{incidentId}")]
        public async Task<IActionResult> GetSimplifiedCaseRecordByIncidentId(int incidentId)
        {
            try
            {
                var record = await _service.GetSimplifiedCaseRecordByIncidentIdAsync(incidentId);
                if (record == null)
                {
                    return NotFound(new { success = false, message = "Simplified case record not found" });
                }
                return Ok(new { success = true, data = record });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving simplified case record for incident: {IncidentID}", incidentId);
                return StatusCode(500, new { success = false, message = "Error retrieving simplified case record" });
            }
        }

        [HttpGet("simplified-escalation-id/{escalationId}")]
        public async Task<IActionResult> GetSimplifiedCaseRecordByEscalationId(int escalationId)
        {
            try
            {
                var record = await _service.GetSimplifiedCaseRecordByEscalationIdAsync(escalationId);
                if (record == null)
                {
                    return NotFound(new { success = false, message = "Simplified case record not found" });
                }
                return Ok(new { success = true, data = record });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving simplified case record for escalation: {EscalationID}", escalationId);
                return StatusCode(500, new { success = false, message = "Error retrieving simplified case record" });
            }
        }

        [HttpGet("count-cases-by-name")]
        public async Task<IActionResult> GetStudentCaseCount([FromQuery] string studentName)
        {
            try
            {
                var count = await _service.CountCasesByStudentNameAsync(studentName);
                return Ok(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting cases for student: {StudentName}", studentName);
                return StatusCode(500, new { success = false, message = "Error counting cases" });
            }
        }
        [HttpPost("simplified")]
        public async Task<IActionResult> CreateSimplifiedCaseRecord([FromBody] SimplifiedStudentProfileCaseRecordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RespondentName))
                {
                    return BadRequest(new { success = false, message = "Respondent name is required" });
                }

                if (string.IsNullOrWhiteSpace(request.ViolationCommitted))
                {
                    return BadRequest(new { success = false, message = "Violation committed is required" });
                }

                var recordId = await _service.CreateSimplifiedCaseRecordAsync(request);
                return Ok(new { success = true, message = "Simplified case record created successfully", data = recordId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating simplified case record");
                return StatusCode(500, new { success = false, message = "Error creating simplified case record" });
            }
        }

        [HttpGet("simplified")]
        public async Task<IActionResult> GetSimplifiedCaseRecords([FromQuery] string? respondentName = null, [FromQuery] string? status = null, [FromQuery] int? incidentId = null)
        {
            try
            {
                var records = await _service.GetSimplifiedCaseRecordsAsync(respondentName, status, incidentId);
                return Ok(new { success = true, data = records });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting simplified case records");
                return StatusCode(500, new { success = false, message = "Error retrieving simplified case records" });
            }
        }

        [HttpGet("simplified/{recordId}")]
        public async Task<IActionResult> GetSimplifiedCaseRecord(int recordId)
        {
            try
            {
                var record = await _service.GetSimplifiedCaseRecordByIdAsync(recordId);
                if (record == null)
                {
                    return NotFound(new { success = false, message = "Simplified case record not found" });
                }
                return Ok(new { success = true, data = record });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting simplified case record");
                return StatusCode(500, new { success = false, message = "Error retrieving simplified case record" });
            }
        }

        // Update parent/guardian info and status for a simplified case record
        [HttpPut("simplified/{recordId}/parent-meeting")]
        public async Task<IActionResult> UpdateParentMeeting(int recordId, [FromBody] ParentMeetingUpdateRequest request)
        {
            _logger.LogInformation("Received UpdateParentMeeting for {RecordID}: Status={Status}, Date={Date}, Type={Type}, Name={Name}", 
                recordId, request?.Status ?? "NULL", request?.ParentMeetingDate?.ToString() ?? "NULL", 
                request?.ParentContactType ?? "NULL", request?.ParentContactName ?? "NULL");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("Invalid model state for record {RecordID}: {Errors}", recordId, errors);
                return BadRequest(new { success = false, message = "Invalid data provided", errors = errors });
            }

            try
            {
                _logger.LogInformation("Updating parent meeting for record {RecordID} with meeting date {MeetingDate}", recordId, request.ParentMeetingDate);
                
                // Verify record exists first
                var record = await _service.GetSimplifiedCaseRecordByIdAsync(recordId);
                if (record == null)
                {
                    _logger.LogWarning("Record {RecordID} not found", recordId);
                    return NotFound(new { success = false, message = $"Record {recordId} not found" });
                }

                _logger.LogInformation("Calling UpdateParentMeetingAsync with: RecordID={RecordID}, MeetingDate={MeetingDate}, ContactType={ContactType}, ContactName={ContactName}, Status={Status}",
                    recordId, request.ParentMeetingDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NULL", 
                    request.ParentContactType ?? "NULL", request.ParentContactName ?? "NULL", request.Status ?? "NULL");

                var success = await _service.UpdateParentMeetingAsync(
                    recordId,
                    request.ParentContactType,
                    request.ParentContactName,
                    request.ParentMeetingDate,
                    request.Status);

                if (!success)
                {
                    _logger.LogWarning("Failed to update record {RecordID} - UpdateParentMeetingAsync returned false", recordId);
                    return BadRequest(new { success = false, message = "Failed to update record" });
                }

                // Verify the update by fetching the record again
                var updatedRecord = await _service.GetSimplifiedCaseRecordByIdAsync(recordId);
                if (updatedRecord != null)
                {
                    _logger.LogInformation("Verification: Record {RecordID} ParentMeetingDate is now: {MeetingDate}", 
                        recordId, updatedRecord.ParentMeetingDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NULL");
                }

                _logger.LogInformation("Successfully updated parent meeting for record {RecordID}", recordId);
                return Ok(new { success = true, message = "Parent meeting updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating parent meeting for record {RecordID}: {Message}", recordId, ex.Message);
                return StatusCode(500, new { success = false, message = $"Error updating parent meeting: {ex.Message}" });
            }
        }
        [HttpPost("send-to-teacher/{recordId}")]
        public async Task<IActionResult> SendToTeacher(int recordId)
        {
            try
            {
                var success = await _service.SendSimplifiedCaseToTeacherAsync(recordId);
                if (success)
                {
                    return Ok(new { success = true, message = "Case sent to teacher successfully" });
                }
                return NotFound(new { success = false, message = "Record not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending case to teacher");
                return StatusCode(500, new { success = false, message = "Error sending case to teacher" });
            }
        }

        /// <summary>
        /// Update Part B fields (ActionTaken, Findings, Agreement, PenaltyAction) for a simplified case record
        /// </summary>
        [HttpPut("simplified/{recordId}/part-b")]
        public async Task<IActionResult> UpdatePartB(int recordId, [FromBody] PartBUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Updating Part B for record {RecordID}", recordId);
                
                // Verify record exists first
                var record = await _service.GetSimplifiedCaseRecordByIdAsync(recordId);
                if (record == null)
                {
                    _logger.LogWarning("Record {RecordID} not found", recordId);
                    return NotFound(new { success = false, message = $"Record {recordId} not found" });
                }

                var success = await _service.UpdatePartBAsync(
                    recordId,
                    request.ActionTaken,
                    request.Findings,
                    request.Agreement,
                    request.PenaltyAction,
                    request.Status);

                if (!success)
                {
                    _logger.LogWarning("Failed to update Part B for record {RecordID}", recordId);
                    return BadRequest(new { success = false, message = "Failed to update record" });
                }

                _logger.LogInformation("Successfully updated Part B for record {RecordID}", recordId);
                return Ok(new { success = true, message = "Part B updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Part B for record {RecordID}: {Message}", recordId, ex.Message);
                return StatusCode(500, new { success = false, message = $"Error updating Part B: {ex.Message}" });
            }
        }

        [HttpGet("simplified/{recordId}/parent-conference-pdf")]
        public async Task<IActionResult> DownloadParentConferencePdf(int recordId)
        {
            try
            {
                var record = await _service.GetSimplifiedCaseRecordByIdAsync(recordId);
                if (record == null)
                {
                    return NotFound(new { success = false, message = "Record not found" });
                }

                // Get POD name from request headers or use default
                string? podName = null;
                if (Request.Headers.TryGetValue("X-POD-Name", out var podNameHeader))
                {
                    podName = podNameHeader.ToString();
                }

                var pdfBytes = _pdfService.GenerateParentConferencePdf(record, podName);
                var fileName = $"ParentConferenceRequest_{record.RespondentName?.Replace(" ", "_") ?? "Student"}_{DateTime.Now:yyyyMMdd}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating parent conference PDF for record {RecordID}", recordId);
                return StatusCode(500, new { success = false, message = "Error generating PDF" });
            }
        }

        /// <summary>
        /// Download Student Profile and Case Record as PDF
        /// </summary>
        [HttpGet("simplified/{recordId}/case-record-pdf")]
        public async Task<IActionResult> DownloadCaseRecordPdf(int recordId)
        {
            try
            {
                var record = await _service.GetSimplifiedCaseRecordByIdAsync(recordId);
                if (record == null)
                {
                    return NotFound(new { success = false, message = "Record not found" });
                }

                var pdfBytes = _caseRecordPdfService.GenerateStudentCaseRecordPdf(
                    record, 
                    record.SchoolName, 
                    record.Region, 
                    record.Division, 
                    record.District
                );
                
                var fileName = $"StudentCaseRecord_{record.RespondentName?.Replace(" ", "_") ?? "Student"}_{DateTime.Now:yyyyMMdd}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating case record PDF for record {RecordID}", recordId);
                return StatusCode(500, new { success = false, message = "Error generating PDF" });
            }
        }

        /// <summary>
        /// Download blank YAKAP Form with student info pre-filled (Name, Grade, Section, Strand, School)
        /// Questions are left blank for manual filling
        /// </summary>
        [HttpGet("simplified/{recordId}/yakap-form-pdf")]
        public async Task<IActionResult> DownloadBlankYakapFormPdf(int recordId)
        {
            try
            {
                var record = await _service.GetSimplifiedCaseRecordByIdAsync(recordId);
                if (record == null)
                {
                    return NotFound(new { success = false, message = "Record not found" });
                }

                var pdfBytes = _yakapPdfService.GenerateBlankYakapFormPdf(record, record.SchoolName ?? "");
                
                var fileName = $"YAKAP_Form_{record.RespondentName?.Replace(" ", "_") ?? "Student"}_{DateTime.Now:yyyyMMdd}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating blank YAKAP form PDF for record {RecordID}", recordId);
                return StatusCode(500, new { success = false, message = "Error generating PDF" });
            }
        }

        /// <summary>
        /// Download Annex B - Intake Sheet as PDF
        /// </summary>
        [HttpGet("simplified/{recordId}/annex-b-pdf")]
        public async Task<IActionResult> DownloadAnnexBPdf(int recordId)
        {
            try
            {
                var record = await _service.GetSimplifiedCaseRecordByIdAsync(recordId);
                if (record == null)
                {
                    return NotFound(new { success = false, message = "Record not found" });
                }

                var pdfBytes = _annexBPdfService.GenerateAnnexBPdf(record);
                var fileName = $"AnnexB_IntakeSheet_{record.RespondentName?.Replace(" ", "_") ?? "Student"}_{DateTime.Now:yyyyMMdd}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Annex B PDF for record {RecordID}", recordId);
                return StatusCode(500, new { success = false, message = "Error generating PDF" });
            }
        }

        [HttpGet("annex-a-pdf")]
        public async Task<IActionResult> DownloadAnnexAPdf(
            [FromQuery] string? status = null, 
            [FromQuery] string? schoolName = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                _logger.LogInformation("Generating Annex A PDF for School: {School}, Status: {Status}, Range: {Start} - {End}", 
                    schoolName, status, startDate?.ToString("yyyy-MM-dd") ?? "START", endDate?.ToString("yyyy-MM-dd") ?? "END");
                
                // Fetch records with date filtering
                var records = await _service.GetSimplifiedCaseRecordsAsync(null, status, null, schoolName, startDate, endDate);
                
                // Get school info from the first record if available
                string region = records.FirstOrDefault()?.Region ?? "";
                string division = records.FirstOrDefault()?.Division ?? "";
                string actualSchoolName = schoolName ?? records.FirstOrDefault()?.SchoolName ?? "";

                var pdfBytes = _annexAPdfService.GenerateAnnexAPdf(records, actualSchoolName, division, region, startDate, endDate);
                var fileName = $"AnnexA_Report_{DateTime.Now:yyyyMMdd}.pdf";
                
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Annex A PDF");
                return StatusCode(500, new { success = false, message = "Error generating PDF" });
            }
        }
    }
}
