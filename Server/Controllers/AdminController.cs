using Microsoft.AspNetCore.Mvc;
using SharedProject;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [HttpPost("create-account")]
        public async Task<ActionResult<ApiResponse<int>>> CreateAccount([FromBody] AdminAccountRequest request)
        {
            try
            {
                // Get current admin ID from claims (will be set after login)
                var adminAccountIdClaim = User.FindFirst("AdminAccountID")?.Value;
                
                if (string.IsNullOrEmpty(adminAccountIdClaim) || !int.TryParse(adminAccountIdClaim, out var createdByAdminId))
                {
                    // For initial admin creation, allow without authentication
                    // In production, you might want to check if any admin exists first
                    createdByAdminId = 0; // System created
                }

                // Validate account type
                var validAccountTypes = new[] { "admin", "schoolhead", "division" };
                if (!validAccountTypes.Contains(request.AccountType.ToLower()))
                {
                    return BadRequest(new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Invalid account type",
                        Errors = new List<string> { "Account type must be: admin, schoolhead, or division" }
                    });
                }

                var result = await _adminService.CreateAdminAccountAsync(request, createdByAdminId);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin account: {Message}", ex.Message);
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("accounts")]
        public async Task<ActionResult<ApiResponse<List<AdminAccount>>>> GetAllAccounts(
            [FromQuery] string? accountType = null,
            [FromQuery] int? createdBy = null)
        {
            try
            {
                var result = await _adminService.GetAllAdminAccountsAsync(accountType, createdBy);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin accounts: {Message}", ex.Message);
                return StatusCode(500, new ApiResponse<List<AdminAccount>>
                {
                    Success = false,
                    Message = "Failed to retrieve admin accounts",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("accounts/{id}")]
        public async Task<ActionResult<ApiResponse<AdminAccount>>> GetAccountById(int id)
        {
            try
            {
                var result = await _adminService.GetAdminAccountByIdAsync(id);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return NotFound(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin account: {Message}", ex.Message);
                return StatusCode(500, new ApiResponse<AdminAccount>
                {
                    Success = false,
                    Message = "Failed to retrieve admin account",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("accounts/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateAccount(int id, [FromBody] AdminAccountUpdateRequest request)
        {
            try
            {
                request.AdminAccountID = id;
                var result = await _adminService.UpdateAdminAccountAsync(id, request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return NotFound(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin account: {Message}", ex.Message);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update admin account",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("accounts/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteAccount(int id)
        {
            try
            {
                var result = await _adminService.DeleteAdminAccountAsync(id);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return NotFound(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting admin account: {Message}", ex.Message);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to delete admin account",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("accounts/by-division/{divisionName}")]
        public async Task<ActionResult<ApiResponse<List<AdminAccount>>>> GetAccountsByDivision(string divisionName)
        {
            try
            {
                var result = await _adminService.GetAccountsByDivisionAsync(divisionName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accounts by division: {Message}", ex.Message);
                return StatusCode(500, new ApiResponse<List<AdminAccount>>
                {
                    Success = false,
                    Message = "Failed to retrieve accounts",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("schools/by-division/{divisionName}")]
        public async Task<ActionResult<ApiResponse<List<School>>>> GetSchoolsByDivision(string divisionName)
        {
            try
            {
                var result = await _adminService.GetSchoolsByDivisionAsync(divisionName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schools by division: {Message}", ex.Message);
                return StatusCode(500, new ApiResponse<List<School>>
                {
                    Success = false,
                    Message = "Failed to retrieve schools",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

