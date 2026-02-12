using Microsoft.AspNetCore.Mvc;
using Server.Services;
using SharedProject;
using System.Text.Json;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YakapFormController : ControllerBase
    {
        private readonly YakapFormService _service;
        private readonly ILogger<YakapFormController> _logger;

        public YakapFormController(YakapFormService service, ILogger<YakapFormController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateYakapForm([FromBody] YakapFormRequest request)
        {
            try
            {
                _logger.LogInformation("Creating Y.A.K.A.P. form for RecordID: {RecordID}, Student: {StudentName}", 
                    request.RecordID, request.StudentName);

                // Validate required fields
                if (request.RecordID <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Record ID is required and must be greater than 0"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.StudentName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Student name is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.FacilitatorCounselor))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Facilitator/Counselor is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.SchoolName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "School name is required"
                    });
                }

                var yakapForm = await _service.CreateYakapFormAsync(request);

                return Ok(new
                {
                    success = true,
                    message = "Y.A.K.A.P. form created successfully",
                    data = yakapForm
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Y.A.K.A.P. form: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating Y.A.K.A.P. form",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{yakapFormId}")]
        public async Task<IActionResult> GetYakapForm(int yakapFormId)
        {
            try
            {
                _logger.LogInformation("Getting Y.A.K.A.P. form with ID: {YakapFormID}", yakapFormId);

                var yakapForm = await _service.GetYakapFormByIdAsync(yakapFormId);

                if (yakapForm == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Y.A.K.A.P. form not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = yakapForm
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Y.A.K.A.P. form: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving Y.A.K.A.P. form",
                    error = ex.Message
                });
            }
        }

        [HttpGet("record/{recordId}")]
        public async Task<IActionResult> GetYakapFormByRecordId(int recordId)
        {
            try
            {
                _logger.LogInformation("Getting Y.A.K.A.P. form for RecordID: {RecordID}", recordId);

                var yakapForm = await _service.GetYakapFormByRecordIdAsync(recordId);

                if (yakapForm == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Y.A.K.A.P. form not found for this record"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = yakapForm
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Y.A.K.A.P. form by RecordID: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving Y.A.K.A.P. form",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllYakapForms([FromQuery] int? recordId = null, [FromQuery] string? studentUsername = null)
        {
            try
            {
                _logger.LogInformation("Getting all Y.A.K.A.P. forms (RecordID: {RecordID}, StudentUsername: {StudentUsername})", recordId, studentUsername);

                List<YakapFormModel> yakapForms;
                
                if (!string.IsNullOrEmpty(studentUsername))
                {
                    yakapForms = await _service.GetYakapFormsByUsernameAsync(studentUsername);
                }
                else
                {
                    yakapForms = await _service.GetAllYakapFormsAsync(recordId);
                }

                return Ok(new
                {
                    success = true,
                    data = yakapForms,
                    count = yakapForms.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Y.A.K.A.P. forms: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving Y.A.K.A.P. forms",
                    error = ex.Message
                });
            }
        }

        [HttpPut("{yakapFormId}")]
        public async Task<IActionResult> UpdateYakapForm(int yakapFormId, [FromBody] YakapFormRequest request)
        {
            try
            {
                _logger.LogInformation("Updating Y.A.K.A.P. form {YakapFormID} for RecordID: {RecordID}", yakapFormId, request.RecordID);

                // Validate required fields
                if (yakapFormId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid Y.A.K.A.P. form ID"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.StudentName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Student name is required"
                    });
                }

                var yakapForm = await _service.UpdateYakapFormAsync(yakapFormId, request);

                if (yakapForm == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Y.A.K.A.P. form not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Y.A.K.A.P. form updated successfully",
                    data = yakapForm
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Y.A.K.A.P. form: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating Y.A.K.A.P. form",
                    error = ex.Message
                });
            }
        }

        [HttpGet("download-pdf/{yakapFormId}")]
        public async Task<IActionResult> DownloadYakapFormPdf(int yakapFormId)
        {
            try
            {
                _logger.LogInformation("Generating PDF for Y.A.K.A.P. form ID: {YakapFormID}", yakapFormId);

                // Get the YAKAP form
                var yakapForm = await _service.GetYakapFormByIdAsync(yakapFormId);
                if (yakapForm == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Y.A.K.A.P. form not found"
                    });
                }

                // Get the related case record for student information
                var caseRecordService = HttpContext.RequestServices.GetService<StudentProfileCaseRecordService>();
                StudentProfileCaseRecordModel? caseRecord = null;
                if (caseRecordService != null && yakapForm.RecordID > 0)
                {
                    caseRecord = await caseRecordService.GetCaseRecordByIdAsync(yakapForm.RecordID);
                }

                // Generate PDF
                var pdfService = HttpContext.RequestServices.GetService<YakapPdfService>();
                if (pdfService == null)
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "PDF service not available"
                    });
                }

                var pdfBytes = pdfService.GenerateYakapFormPdf(yakapForm, caseRecord);

                // Return PDF file
                var fileName = $"YAKAP_Form_{yakapForm.StudentName?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for Y.A.K.A.P. form: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating PDF",
                    error = ex.Message
                });
            }
        }
    }
}

