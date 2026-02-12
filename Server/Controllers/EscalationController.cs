using Microsoft.AspNetCore.Mvc;
using Server.Services;
using SharedProject;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EscalationController : ControllerBase
    {
        private readonly EscalationService _service;
        private readonly ILogger<EscalationController> _logger;

        public EscalationController(EscalationService service, ILogger<EscalationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("escalate")]
        public async Task<IActionResult> EscalateStudent([FromBody] EscalateStudentRequest request, [FromQuery] string escalatedBy, [FromQuery] int teacherId, [FromQuery] int? schoolId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.StudentName))
                {
                    return BadRequest(new { success = false, message = "Student name is required" });
                }

                if (string.IsNullOrWhiteSpace(escalatedBy))
                {
                    return BadRequest(new { success = false, message = "Escalated by is required" });
                }

                if (request.MinorCaseCount < 3)
                {
                    return BadRequest(new { success = false, message = "Student must have at least 3 minor cases to escalate" });
                }

                _logger.LogInformation("Escalating student {StudentName} by {EscalatedBy}", request.StudentName, escalatedBy);

                var escalationId = await _service.EscalateStudentToPODAsync(request, escalatedBy, teacherId, schoolId);

                return Ok(new
                {
                    success = true,
                    message = $"Successfully escalated {request.StudentName} to POD",
                    data = new { escalationId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalating student");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error escalating student: {ex.Message}"
                });
            }
        }

        [HttpGet("pod")]
        public async Task<IActionResult> GetEscalatedStudentsForPOD([FromQuery] int? schoolId = null, [FromQuery] string? status = null)
        {
            try
            {
                _logger.LogInformation("Getting escalated students for POD. SchoolID: {SchoolID}, Status: {Status}", schoolId, status);

                var escalations = await _service.GetEscalatedStudentsForPODAsync(schoolId, status);

                return Ok(new
                {
                    success = true,
                    data = escalations,
                    message = $"Found {escalations.Count} escalated students"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalated students for POD");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving escalated students: {ex.Message}"
                });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetEscalationStatus([FromQuery] string studentName, [FromQuery] string gradeLevel, [FromQuery] string section)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(gradeLevel) || string.IsNullOrWhiteSpace(section))
                {
                    return BadRequest(new { success = false, message = "Student name, grade level, and section are required" });
                }

                var escalation = await _service.GetEscalationStatusAsync(studentName, gradeLevel, section);

                if (escalation == null)
                {
                    return Ok(new
                    {
                        success = true,
                        isEscalated = false,
                        data = (CaseEscalation?)null
                    });
                }

                return Ok(new
                {
                    success = true,
                    isEscalated = true,
                    data = escalation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking escalation status");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error checking escalation status: {ex.Message}"
                });
            }
        }

        [HttpDelete("{escalationId}")]
        public async Task<IActionResult> WithdrawEscalation(int escalationId, [FromQuery] int teacherId)
        {
            try
            {
                _logger.LogInformation("Withdrawing escalation {EscalationID} by teacher {TeacherID}", escalationId, teacherId);

                var success = await _service.WithdrawEscalationAsync(escalationId, teacherId);

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Escalation not found or you don't have permission to withdraw it"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Successfully withdrew escalation"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing escalation");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error withdrawing escalation: {ex.Message}"
                });
            }
        }


        [HttpPut("{escalationId}/status")]
        public async Task<IActionResult> UpdateStatus(int escalationId, [FromQuery] string status)
        {
            try
            {
                _logger.LogInformation("Updating escalation {EscalationID} status to {Status}", escalationId, status);

                if (string.IsNullOrWhiteSpace(status))
                {
                    return BadRequest(new { success = false, message = "Status is required" });
                }

                // Validate that the status is a valid enum value
                if (!Enum.TryParse<EscalationStatus>(status, true, out var _))
                {
                    var validStatuses = string.Join(", ", Enum.GetNames(typeof(EscalationStatus)));
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = $"Invalid status value. Valid values are: {validStatuses}" 
                    });
                }

                var success = await _service.UpdateEscalationStatusAsync(escalationId, status);

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Escalation not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"Successfully updated status to {status}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating escalation status");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error updating status: {ex.Message}"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEscalatedCase(int id)
        {
            try
            {
                _logger.LogInformation("Getting escalated case details for ID: {EscalationID}", id);

                var escalation = await _service.GetEscalatedCaseByIdAsync(id);

                if (escalation == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Escalation not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = escalation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalated case details");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving escalation details: {ex.Message}"
                });
            }
        }
    }
}
