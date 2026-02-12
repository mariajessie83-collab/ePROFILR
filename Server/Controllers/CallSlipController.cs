using Microsoft.AspNetCore.Mvc;
using Server.Services;
using System.Text.Json;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallSlipController : ControllerBase
    {
        private readonly CallSlipReportService _callSlipService;
        private readonly CallSlipService _callSlipDataService;
        private readonly ILogger<CallSlipController> _logger;

        public CallSlipController(CallSlipReportService callSlipService, CallSlipService callSlipDataService, ILogger<CallSlipController> logger)
        {
            _callSlipService = callSlipService;
            _callSlipDataService = callSlipDataService;
            _logger = logger;
        }

        [HttpGet("generate/{incidentId}")]
        public async Task<IActionResult> GenerateCallSlip(int incidentId, [FromQuery] DateTime? date = null, [FromQuery] string? time = null, [FromQuery] string? generatedBy = null)
        {
            try
            {
                _logger.LogInformation($"Generating call slip for incident ID: {incidentId}");
                TimeSpan? timeSpan = null;
                if (!string.IsNullOrWhiteSpace(time) && TimeSpan.TryParse(time, out var ts))
                {
                    timeSpan = ts;
                }

                var pdfBytes = await _callSlipService.GenerateCallSlipReportAsync(incidentId, date, timeSpan, generatedBy);
                
                // Save call slip record to database
                _logger.LogInformation($"Attempting to save call slip for IncidentID: {incidentId}, GeneratedBy: {generatedBy ?? "System"}");
                
                try
                {
                    _logger.LogInformation($"Calling GetCallSlipModelAsync for IncidentID: {incidentId}");
                    var callSlipModel = await _callSlipService.GetCallSlipModelAsync(incidentId, date, timeSpan, generatedBy);
                    
                    if (callSlipModel != null)
                    {
                        _logger.LogInformation($"Call slip model retrieved successfully. Complainant: {callSlipModel.ComplainantName}, School: {callSlipModel.SchoolName}");
                        
                        callSlipModel.GeneratedBy = generatedBy ?? "System";
                        callSlipModel.MeetingDate = date;
                        callSlipModel.MeetingTime = timeSpan;
                        
                        _logger.LogInformation($"Calling SaveCallSlipAsync for IncidentID: {incidentId}");
                        var callSlipId = await _callSlipDataService.SaveCallSlipAsync(callSlipModel);
                        _logger.LogInformation($"Call slip saved successfully with ID: {callSlipId} for IncidentID: {incidentId}");
                    }
                    else
                    {
                        _logger.LogError($"Call slip model is NULL for IncidentID: {incidentId}. Cannot save to database.");
                    }
                }
                catch (Exception saveEx)
                {
                    // Log the full error details - this is critical!
                    _logger.LogError(saveEx, $"CRITICAL: Failed to save call slip record for IncidentID: {incidentId}");
                    _logger.LogError($"Error Message: {saveEx.Message}");
                    _logger.LogError($"Error Source: {saveEx.Source}");
                    _logger.LogError($"Inner Exception: {saveEx.InnerException?.Message ?? "None"}");
                    _logger.LogError($"Stack Trace: {saveEx.StackTrace}");
                    
                    // Check if it's a table doesn't exist error
                    if (saveEx.Message.Contains("doesn't exist") || saveEx.Message.Contains("Table") || 
                        saveEx.Message.Contains("callslips") || saveEx.Message.Contains("Unknown table"))
                    {
                        _logger.LogError("*** TABLE MISSING ERROR *** Callslips table does not exist. Please run the SQL script: Server/Data/callslips_table_schema.sql");
                    }
                    
                    // Don't rethrow - let PDF generation succeed even if save fails
                }

                var fileName = $"CallSlip_{incidentId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating call slip for incident ID: {incidentId}");
                
                return BadRequest(new
                {
                    success = false,
                    message = $"Error generating call slip: {ex.Message}"
                });
            }
        }

        [HttpGet("generate-escalation/{escalationId}")]
        public async Task<IActionResult> GenerateEscalationCallSlip(int escalationId, [FromQuery] DateTime? date = null, [FromQuery] string? time = null, [FromQuery] string? generatedBy = null)
        {
            try
            {
                _logger.LogInformation($"Generating call slip for escalation ID: {escalationId}");
                TimeSpan? timeSpan = null;
                if (!string.IsNullOrWhiteSpace(time) && TimeSpan.TryParse(time, out var ts))
                {
                    timeSpan = ts;
                }

                var pdfBytes = await _callSlipService.GenerateEscalationCallSlipReportAsync(escalationId, date, timeSpan, generatedBy);
                
                // Save call slip record to database
                _logger.LogInformation($"Attempting to save call slip for EscalationID: {escalationId}, GeneratedBy: {generatedBy ?? "System"}");
                
                try
                {
                    _logger.LogInformation($"Calling GetEscalationCallSlipModelAsync for EscalationID: {escalationId}");
                    var callSlipModel = await _callSlipService.GetEscalationCallSlipModelAsync(escalationId, date, timeSpan, generatedBy);
                    
                    if (callSlipModel != null)
                    {
                        callSlipModel.GeneratedBy = generatedBy ?? "System";
                        callSlipModel.MeetingDate = date;
                        callSlipModel.MeetingTime = timeSpan;
                        callSlipModel.IncidentID = 0; // Set to 0 so it becomes NULL in DB, avoiding FK violation
                        callSlipModel.EscalationID = escalationId; // Correctly use the EscalationID column
                        
                        _logger.LogInformation($"Calling SaveCallSlipAsync for EscalationID: {escalationId}");
                        var callSlipId = await _callSlipDataService.SaveCallSlipAsync(callSlipModel);
                        _logger.LogInformation($"Call slip saved successfully with ID: {callSlipId} for EscalationID: {escalationId}");
                    }
                    else
                    {
                        _logger.LogError($"Call slip model is NULL for EscalationID: {escalationId}. Cannot save to database.");
                    }
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, $"CRITICAL: Failed to save call slip record for EscalationID: {escalationId}");
                    // Don't re-throw - let PDF generation succeed even if save fails, matching regular GenerateCallSlip behavior
                }

                var fileName = $"CallSlip_Escalation_{escalationId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating call slip for escalation ID: {escalationId}");
                
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("preview/{incidentId}")]
        public async Task<IActionResult> PreviewCallSlip(int incidentId)
        {
            try
            {
                _logger.LogInformation($"Previewing call slip for incident ID: {incidentId}");

                var htmlBytes = await _callSlipService.GenerateCallSlipReportAsync(incidentId);
                
                return File(htmlBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error previewing call slip for incident ID: {incidentId}");
                
                return BadRequest(new
                {
                    success = false,
                    message = $"Error previewing call slip: {ex.Message}"
                });
            }
        }

        [HttpGet("test-table")]
        public async Task<IActionResult> TestTable()
        {
            try
            {
                var result = await _callSlipDataService.TestTableExistsAsync();
                
                return Ok(new
                {
                    success = true,
                    tableExists = result.TableExists,
                    message = result.Message,
                    error = result.Error
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing callslips table");
                
                return BadRequest(new
                {
                    success = false,
                    message = $"Error testing table: {ex.Message}"
                });
            }
        }

        [HttpGet("incident/{incidentId}")]
        public async Task<IActionResult> GetCallSlipsByIncidentId(int incidentId)
        {
            try
            {
                _logger.LogInformation($"Fetching call slips for incident ID: {incidentId}");
                var callSlips = await _callSlipDataService.GetCallSlipsByIncidentIdAsync(incidentId);
                
                return Ok(new
                {
                    success = true,
                    data = callSlips
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching call slips for incident ID: {incidentId}");
                
                return BadRequest(new
                {
                    success = false,
                    message = $"Error fetching call slips: {ex.Message}"
                });
            }
        }

        [HttpGet("escalation/{escalationId}")]
        public async Task<IActionResult> GetCallSlipsByEscalationId(int escalationId)
        {
            try
            {
                _logger.LogInformation($"Fetching call slips for escalation ID: {escalationId}");
                var callSlips = await _callSlipDataService.GetCallSlipsByEscalationIdAsync(escalationId);
                
                return Ok(new
                {
                    success = true,
                    data = callSlips
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching call slips for escalation ID: {escalationId}");
                
                return BadRequest(new
                {
                    success = false,
                    message = $"Error fetching call slips: {ex.Message}"
                });
            }
        }
    }
}
