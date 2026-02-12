using Microsoft.AspNetCore.Mvc;
using Server.Services;
using System.Text.Json;
using System.Data;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallSlipReportController : ControllerBase
    {
        private readonly CallSlipReportService _callSlipService;
        private readonly ILogger<CallSlipReportController> _logger;

        public CallSlipReportController(CallSlipReportService callSlipService, ILogger<CallSlipReportController> logger)
        {
            _callSlipService = callSlipService;
            _logger = logger;
        }

        [HttpGet("data/{incidentId}")]
        public async Task<IActionResult> GetCallSlipData(int incidentId)
        {
            try
            {
                _logger.LogInformation($"Getting call slip data for incident ID: {incidentId}");

                var dataTable = await _callSlipService.GetCallSlipDataAsync(incidentId);
                
                if (dataTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No data found for incident ID: {incidentId}"
                    });
                }

                // Convert DataTable to JSON
                var data = new List<object>();
                foreach (DataRow row in dataTable.Rows)
                {
                    data.Add(new
                    {
                        IncidentId = row["IncidentId"],
                        ComplainantName = row["ComplainantName"]?.ToString(),
                        VictimName = row["VictimName"]?.ToString(),
                        RespondentName = row["RespondentName"]?.ToString(),
                        DateReported = row["DateReported"],
                        TimeReported = row["TimeReported"],
                        SchoolName = row["SchoolName"]?.ToString(),
                        GradeLevel = row["GradeLevel"]?.ToString(),
                        Section = row["Section"]?.ToString(),
                        RoomNumber = row["RoomNumber"]?.ToString(),
                        Status = row["Status"]?.ToString(),
                        PODTeacherName = row["TeacherName"]?.ToString(),
                        PODPosition = row["Position"]?.ToString()
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = data.FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting call slip data for incident ID: {incidentId}");
                
                return BadRequest(new
                {
                    success = false,
                    message = $"Error getting call slip data: {ex.Message}"
                });
            }
        }

        [HttpGet("generate/{incidentId}")]
        public async Task<IActionResult> GenerateCallSlip(int incidentId)
        {
            try
            {
                _logger.LogInformation($"Generating call slip for incident ID: {incidentId}");

                var htmlBytes = await _callSlipService.GenerateCallSlipReportAsync(incidentId);
                
                var fileName = $"CallSlip_{incidentId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                
                return File(htmlBytes, "application/pdf", fileName);
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
    }
}
