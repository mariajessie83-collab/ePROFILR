using SharedProject;
using Server.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Server.Services
{
    public class AdminService
    {
        private readonly Dbconnections _dbConnections;
        private readonly ILogger<AdminService> _logger;

        public AdminService(Dbconnections dbConnections, ILogger<AdminService> logger)
        {
            _dbConnections = dbConnections;
            _logger = logger;
        }

        public async Task<ApiResponse<int>> CreateAdminAccountAsync(AdminAccountRequest request, int createdByAdminId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var username = await GenerateUniqueUsernameAsync(connection, request.Username, request.FullName);
                var passwordWasProvided = !string.IsNullOrWhiteSpace(request.Password);
                var passwordToStore = passwordWasProvided ? request.Password! : GenerateSecurePassword();

                // Validate account type specific fields
                if (request.AccountType.ToLower() == "schoolhead")
                {
                    if (string.IsNullOrWhiteSpace(request.SchoolName))
                    {
                        return new ApiResponse<int>
                        {
                            Success = false,
                            Message = "School name is required for school head accounts",
                            Errors = new List<string> { "School name is required" }
                        };
                    }
                }
                else if (request.AccountType.ToLower() == "division")
                {
                    if (string.IsNullOrWhiteSpace(request.Division))
                    {
                        return new ApiResponse<int>
                        {
                            Success = false,
                            Message = "Division name is required for division accounts",
                            Errors = new List<string> { "Division name is required" }
                        };
                    }
                }

                // Determine UserRole based on AccountType
                string userRole;
                switch (request.AccountType.ToLower())
                {
                    case "admin":
                        userRole = "admin";
                        break;
                    case "schoolhead":
                        userRole = "schoolhead";
                        break;
                    case "division":
                        userRole = "division";
                        break;
                    default:
                        userRole = "admin";
                        break;
                }

                // Create User account first
                var insertUserQuery = @"
                    INSERT INTO Users (Username, Password, UserRole, IsActive, DateCreated)
                    VALUES (@Username, @Password, @UserRole, 1, NOW())";

                using var insertUserCmd = new MySqlCommand(insertUserQuery, connection);
                insertUserCmd.Parameters.AddWithValue("@Username", username);
                insertUserCmd.Parameters.AddWithValue("@Password", passwordToStore);
                insertUserCmd.Parameters.AddWithValue("@UserRole", userRole);
                
                await insertUserCmd.ExecuteNonQueryAsync();
                var userId = (int)insertUserCmd.LastInsertedId;

                // Create AdminAccount
                var insertAdminQuery = @"
                    INSERT INTO AdminAccounts (
                        UserID, AccountType, FullName, Email, PhoneNumber,
                        SchoolID, SchoolName, School_ID, Division, Region, District,
                        DivisionName, IsActive, CreatedBy, DateCreated
                    )
                    VALUES (
                        @UserID, @AccountType, @FullName, @Email, @PhoneNumber,
                        @SchoolID, @SchoolName, @School_ID, @Division, @Region, @District,
                        @DivisionName, 1, @CreatedBy, NOW()
                    )";

                using var insertAdminCmd = new MySqlCommand(insertAdminQuery, connection);
                insertAdminCmd.Parameters.AddWithValue("@UserID", userId);
                insertAdminCmd.Parameters.AddWithValue("@AccountType", request.AccountType.ToLower());
                insertAdminCmd.Parameters.AddWithValue("@FullName", request.FullName);
                insertAdminCmd.Parameters.AddWithValue("@Email", request.Email ?? (object)DBNull.Value);
                insertAdminCmd.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber ?? (object)DBNull.Value);
                insertAdminCmd.Parameters.AddWithValue("@SchoolID", request.SchoolID ?? (object)DBNull.Value);
                insertAdminCmd.Parameters.AddWithValue("@SchoolName", request.SchoolName ?? (object)DBNull.Value);
                insertAdminCmd.Parameters.AddWithValue("@School_ID", request.School_ID ?? (object)DBNull.Value);
                // For division accounts, save to Division column (not DivisionName)
                var divisionValue = request.Division ?? request.DivisionName;
                insertAdminCmd.Parameters.AddWithValue("@Division", divisionValue ?? (object)DBNull.Value);
                insertAdminCmd.Parameters.AddWithValue("@Region", request.Region ?? (object)DBNull.Value);
                insertAdminCmd.Parameters.AddWithValue("@District", request.District ?? (object)DBNull.Value);
                insertAdminCmd.Parameters.AddWithValue("@DivisionName", (object)DBNull.Value);
                insertAdminCmd.Parameters.AddWithValue("@CreatedBy", createdByAdminId > 0 ? createdByAdminId : (object)DBNull.Value);

                await insertAdminCmd.ExecuteNonQueryAsync();
                var adminAccountId = (int)insertAdminCmd.LastInsertedId;

                _logger.LogInformation("Admin account created: {AdminAccountID}, Type: {AccountType}, CreatedBy: {CreatedBy}",
                    adminAccountId, request.AccountType, createdByAdminId);

                var successMessage = $"{request.AccountType} account created successfully. Username: {username}";

                if (!passwordWasProvided)
                {
                    successMessage += $", Password: {passwordToStore}";
                }

                return new ApiResponse<int>
                {
                    Success = true,
                    Message = successMessage,
                    Data = adminAccountId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin account: {Message}", ex.Message);
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = "Failed to create admin account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<List<AdminAccount>>> GetAllAdminAccountsAsync(string? accountType = null, int? createdBy = null)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        aa.AdminAccountID, aa.UserID, aa.AccountType, aa.FullName, 
                        aa.Email, aa.PhoneNumber, aa.SchoolID, aa.SchoolName, aa.School_ID,
                        aa.Division, aa.Region, aa.District, aa.DivisionName,
                        aa.IsActive, aa.CreatedBy, aa.DateCreated, aa.LastModified,
                        u.Username,
                        u.Password as UserPassword,
                        creator.FullName as CreatorName
                    FROM AdminAccounts aa
                    INNER JOIN Users u ON aa.UserID = u.UserID
                    LEFT JOIN AdminAccounts creator ON aa.CreatedBy = creator.AdminAccountID
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(accountType))
                {
                    query += " AND aa.AccountType = @AccountType";
                }

                if (createdBy.HasValue)
                {
                    query += " AND aa.CreatedBy = @CreatedBy";
                }

                query += " ORDER BY aa.DateCreated DESC";

                using var command = new MySqlCommand(query, connection);
                
                if (!string.IsNullOrEmpty(accountType))
                {
                    command.Parameters.AddWithValue("@AccountType", accountType.ToLower());
                }
                
                if (createdBy.HasValue)
                {
                    command.Parameters.AddWithValue("@CreatedBy", createdBy.Value);
                }

                var accounts = new List<AdminAccount>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    accounts.Add(new AdminAccount
                    {
                        AdminAccountID = reader.GetInt32("AdminAccountID"),
                        UserID = reader.GetInt32("UserID"),
                        AccountType = reader.GetString("AccountType"),
                        FullName = reader.GetString("FullName"),
                        Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        School_ID = reader.IsDBNull("School_ID") ? null : reader.GetString("School_ID"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        DivisionName = reader.IsDBNull("DivisionName") ? null : reader.GetString("DivisionName"),
                        IsActive = reader.GetBoolean("IsActive"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetInt32("CreatedBy"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        LastModified = reader.IsDBNull("LastModified") ? null : reader.GetDateTime("LastModified"),
                        Username = reader.GetString("Username"),
                        UserPassword = reader.IsDBNull("UserPassword") ? null : reader.GetString("UserPassword"),
                        CreatorName = reader.IsDBNull("CreatorName") ? null : reader.GetString("CreatorName")
                    });
                }

                return new ApiResponse<List<AdminAccount>>
                {
                    Success = true,
                    Message = "Admin accounts retrieved successfully",
                    Data = accounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin accounts: {Message}", ex.Message);
                return new ApiResponse<List<AdminAccount>>
                {
                    Success = false,
                    Message = "Failed to retrieve admin accounts",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<AdminAccount>> GetAdminAccountByIdAsync(int adminAccountId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        aa.AdminAccountID, aa.UserID, aa.AccountType, aa.FullName, 
                        aa.Email, aa.PhoneNumber, aa.SchoolID, aa.SchoolName, aa.School_ID,
                        aa.Division, aa.Region, aa.District, aa.DivisionName,
                        aa.IsActive, aa.CreatedBy, aa.DateCreated, aa.LastModified,
                        u.Username,
                        u.Password as UserPassword,
                        creator.FullName as CreatorName
                    FROM AdminAccounts aa
                    INNER JOIN Users u ON aa.UserID = u.UserID
                    LEFT JOIN AdminAccounts creator ON aa.CreatedBy = creator.AdminAccountID
                    WHERE aa.AdminAccountID = @AdminAccountID";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdminAccountID", adminAccountId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var account = new AdminAccount
                    {
                        AdminAccountID = reader.GetInt32("AdminAccountID"),
                        UserID = reader.GetInt32("UserID"),
                        AccountType = reader.GetString("AccountType"),
                        FullName = reader.GetString("FullName"),
                        Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        School_ID = reader.IsDBNull("School_ID") ? null : reader.GetString("School_ID"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        DivisionName = reader.IsDBNull("DivisionName") ? null : reader.GetString("DivisionName"),
                        IsActive = reader.GetBoolean("IsActive"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetInt32("CreatedBy"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        LastModified = reader.IsDBNull("LastModified") ? null : reader.GetDateTime("LastModified"),
                        Username = reader.GetString("Username"),
                        UserPassword = reader.IsDBNull("UserPassword") ? null : reader.GetString("UserPassword"),
                        CreatorName = reader.IsDBNull("CreatorName") ? null : reader.GetString("CreatorName")
                    };

                    return new ApiResponse<AdminAccount>
                    {
                        Success = true,
                        Message = "Admin account retrieved successfully",
                        Data = account
                    };
                }

                return new ApiResponse<AdminAccount>
                {
                    Success = false,
                    Message = "Admin account not found",
                    Errors = new List<string> { "Account not found" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin account: {Message}", ex.Message);
                return new ApiResponse<AdminAccount>
                {
                    Success = false,
                    Message = "Failed to retrieve admin account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<AdminAccount>> GetAdminAccountByUserIdAsync(int userId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        aa.AdminAccountID, aa.UserID, aa.AccountType, aa.FullName, 
                        aa.Email, aa.PhoneNumber, aa.SchoolID, aa.SchoolName, aa.School_ID,
                        aa.Division, aa.Region, aa.District, aa.DivisionName,
                        aa.IsActive, aa.CreatedBy, aa.DateCreated, aa.LastModified,
                        u.Username,
                        u.Password as UserPassword,
                        creator.FullName as CreatorName
                    FROM AdminAccounts aa
                    INNER JOIN Users u ON aa.UserID = u.UserID
                    LEFT JOIN AdminAccounts creator ON aa.CreatedBy = creator.AdminAccountID
                    WHERE aa.UserID = @UserID";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserID", userId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var account = new AdminAccount
                    {
                        AdminAccountID = reader.GetInt32("AdminAccountID"),
                        UserID = reader.GetInt32("UserID"),
                        AccountType = reader.GetString("AccountType"),
                        FullName = reader.GetString("FullName"),
                        Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        School_ID = reader.IsDBNull("School_ID") ? null : reader.GetString("School_ID"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        DivisionName = reader.IsDBNull("DivisionName") ? null : reader.GetString("DivisionName"),
                        IsActive = reader.GetBoolean("IsActive"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetInt32("CreatedBy"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        LastModified = reader.IsDBNull("LastModified") ? null : reader.GetDateTime("LastModified"),
                        Username = reader.GetString("Username"),
                        UserPassword = reader.IsDBNull("UserPassword") ? null : reader.GetString("UserPassword"),
                        CreatorName = reader.IsDBNull("CreatorName") ? null : reader.GetString("CreatorName")
                    };

                    return new ApiResponse<AdminAccount>
                    {
                        Success = true,
                        Message = "Admin account retrieved successfully",
                        Data = account
                    };
                }

                return new ApiResponse<AdminAccount>
                {
                    Success = false,
                    Message = "Admin account not found",
                    Errors = new List<string> { "Account not found" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin account by UserID: {Message}", ex.Message);
                return new ApiResponse<AdminAccount>
                {
                    Success = false,
                    Message = "Failed to retrieve admin account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> UpdateAdminAccountAsync(int adminAccountId, AdminAccountUpdateRequest request)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var updateQuery = @"
                    UPDATE AdminAccounts 
                    SET FullName = COALESCE(@FullName, FullName),
                        Email = @Email,
                        PhoneNumber = @PhoneNumber,
                        SchoolID = @SchoolID,
                        SchoolName = @SchoolName,
                        School_ID = @School_ID,
                        Division = @Division,
                        Region = @Region,
                        District = @District,
                        DivisionName = @DivisionName,
                        IsActive = COALESCE(@IsActive, IsActive),
                        LastModified = NOW()
                    WHERE AdminAccountID = @AdminAccountID";

                using var command = new MySqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@AdminAccountID", adminAccountId);
                command.Parameters.AddWithValue("@FullName", request.FullName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Email", request.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SchoolID", request.SchoolID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SchoolName", request.SchoolName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@School_ID", request.School_ID ?? (object)DBNull.Value);
                
                // Ensure Division column is always updated correctly, preferring Division property then DivisionName
                var divisionToStore = request.Division ?? request.DivisionName;
                command.Parameters.AddWithValue("@Division", divisionToStore ?? (object)DBNull.Value);
                
                command.Parameters.AddWithValue("@Region", request.Region ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@District", request.District ?? (object)DBNull.Value);
                
                // Keep DivisionName for backward compatibility but Division is the primary one now
                command.Parameters.AddWithValue("@DivisionName", request.DivisionName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IsActive", request.IsActive ?? (object)DBNull.Value);

                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Admin account updated: {AdminAccountID}", adminAccountId);
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "Admin account updated successfully",
                        Data = true
                    };
                }

                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Admin account not found",
                    Errors = new List<string> { "Account not found" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin account: {Message}", ex.Message);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update admin account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAdminAccountAsync(int adminAccountId)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Soft delete - set IsActive to false
                var updateQuery = @"
                    UPDATE AdminAccounts 
                    SET IsActive = 0, LastModified = NOW()
                    WHERE AdminAccountID = @AdminAccountID";

                using var command = new MySqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@AdminAccountID", adminAccountId);

                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Admin account deactivated: {AdminAccountID}", adminAccountId);
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "Admin account deactivated successfully",
                        Data = true
                    };
                }

                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Admin account not found",
                    Errors = new List<string> { "Account not found" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting admin account: {Message}", ex.Message);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to delete admin account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<List<AdminAccount>>> GetAccountsByDivisionAsync(string divisionName)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        aa.AdminAccountID, aa.UserID, aa.AccountType, aa.FullName, 
                        aa.Email, aa.PhoneNumber, aa.SchoolID, aa.SchoolName, aa.School_ID,
                        aa.Division, aa.Region, aa.District, aa.DivisionName,
                        aa.IsActive, aa.CreatedBy, aa.DateCreated, aa.LastModified,
                        u.Username,
                        u.Password as UserPassword,
                        creator.FullName as CreatorName
                    FROM AdminAccounts aa
                    INNER JOIN Users u ON aa.UserID = u.UserID
                    LEFT JOIN AdminAccounts creator ON aa.CreatedBy = creator.AdminAccountID
                    WHERE (aa.Division = @DivisionName OR aa.DivisionName = @DivisionName)
                    AND aa.IsActive = 1
                    ORDER BY aa.DateCreated DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@DivisionName", divisionName);

                var accounts = new List<AdminAccount>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    accounts.Add(new AdminAccount
                    {
                        AdminAccountID = reader.GetInt32("AdminAccountID"),
                        UserID = reader.GetInt32("UserID"),
                        AccountType = reader.GetString("AccountType"),
                        FullName = reader.GetString("FullName"),
                        Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        School_ID = reader.IsDBNull("School_ID") ? null : reader.GetString("School_ID"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        DivisionName = reader.IsDBNull("DivisionName") ? null : reader.GetString("DivisionName"),
                        IsActive = reader.GetBoolean("IsActive"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetInt32("CreatedBy"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        LastModified = reader.IsDBNull("LastModified") ? null : reader.GetDateTime("LastModified"),
                        Username = reader.GetString("Username"),
                        UserPassword = reader.IsDBNull("UserPassword") ? null : reader.GetString("UserPassword"),
                        CreatorName = reader.IsDBNull("CreatorName") ? null : reader.GetString("CreatorName")
                    });
                }

                return new ApiResponse<List<AdminAccount>>
                {
                    Success = true,
                    Message = "Accounts retrieved successfully",
                    Data = accounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accounts by division: {Message}", ex.Message);
                return new ApiResponse<List<AdminAccount>>
                {
                    Success = false,
                    Message = "Failed to retrieve accounts",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        private async Task<string> GenerateUniqueUsernameAsync(MySqlConnection connection, string? requestedUsername, string? fullName)
        {
            var baseUsername = SanitizeUsername(requestedUsername);

            if (string.IsNullOrWhiteSpace(baseUsername))
            {
                baseUsername = DeriveUsernameFromFullName(fullName);
            }

            if (string.IsNullOrWhiteSpace(baseUsername))
            {
                baseUsername = $"admin{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            var candidate = baseUsername;
            var counter = 1;

            while (await UsernameExistsAsync(connection, candidate))
            {
                candidate = $"{baseUsername}{counter}";
                counter++;
            }

            return candidate;
        }

        private static string SanitizeUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return string.Empty;
            }

            var sanitized = new string(username
                .Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-')
                .ToArray());

            return sanitized.ToLowerInvariant();
        }

        private static string DeriveUsernameFromFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return string.Empty;
            }

            var normalized = fullName.Replace(",", " ").ToLowerInvariant();
            var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                return string.Empty;
            }

            var lastName = tokens[0];
            var firstName = tokens.Length > 1 ? tokens[1] : tokens[0];

            var baseUsername = $"{firstName}.{lastName}";
            return new string(baseUsername.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray()).Trim('.');
        }

        private static async Task<bool> UsernameExistsAsync(MySqlConnection connection, string username)
        {
            const string query = "SELECT COUNT(1) FROM Users WHERE Username = @Username";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private static string GenerateSecurePassword(int length = 12)
        {
            const string allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789@#$%!";
            var buffer = new byte[length];
            RandomNumberGenerator.Fill(buffer);
            var builder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                var index = buffer[i] % allowedChars.Length;
                builder.Append(allowedChars[index]);
            }

            return builder.ToString();
        }

        public async Task<ApiResponse<List<School>>> GetSchoolsByDivisionAsync(string divisionName)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Use a more robust query to handle trailing spaces and naming variations
                // Use 'Schools' (capitalized) for consistency with TeacherService
                var query = @"
                    SELECT SchoolID, SchoolName, Division, Region, District, IsActive
                    FROM Schools
                    WHERE (TRIM(Division) = TRIM(@DivisionName) 
                       OR TRIM(Division) LIKE CONCAT(TRIM(@DivisionName), ' %')
                       OR TRIM(Division) LIKE CONCAT('% ', TRIM(@DivisionName)))
                      AND IsActive = 1
                    ORDER BY SchoolName";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@DivisionName", divisionName);

                var schools = new List<School>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    schools.Add(new School
                    {
                        SchoolID = reader.GetInt32("SchoolID"),
                        SchoolName = reader.GetString("SchoolName"),
                        Division = reader.IsDBNull("Division") ? null : reader.GetString("Division"),
                        Region = reader.IsDBNull("Region") ? null : reader.GetString("Region"),
                        District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                        IsActive = reader.GetBoolean("IsActive")
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
                _logger.LogError(ex, "Error retrieving schools by division: {Message}", ex.Message);
                return new ApiResponse<List<School>>
                {
                    Success = false,
                    Message = "Failed to retrieve schools",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}

