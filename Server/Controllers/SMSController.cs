using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SMSController : ControllerBase
    {
        private readonly SMSService _smsService;
        private readonly ILogger<SMSController> _logger;

        public SMSController(SMSService smsService, ILogger<SMSController> logger)
        {
            _smsService = smsService;
            _logger = logger;
        }

        [HttpPost("SendSMS")]
        public async Task<IActionResult> SendSMS([FromBody] SMSRequest request)
        {
            try
            {
                _logger.LogInformation($"Sending SMS to {request.PhoneNumber} for record {request.RecordId}");

                if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Message))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Phone number and message are required"
                    });
                }

                var result = await _smsService.SendSMSAsync(request.PhoneNumber, request.Message);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "SMS sent successfully"
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "Failed to send SMS. Please check GSM modem connection."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendSMS endpoint: {Exception}", ex);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error sending SMS: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("TestConnection")]
        public IActionResult TestConnection()
        {
            try
            {
                // Check if COM ports are available
                var ports = System.IO.Ports.SerialPort.GetPortNames();
                return Ok(new
                {
                    success = true,
                    availablePorts = ports,
                    message = $"Found {ports.Length} COM port(s)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error checking COM ports: {ex.Message}"
                });
            }
        }
    }

    public class SMSRequest
    {
        public int RecordId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
    }
}

