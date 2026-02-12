using MySql.Data.MySqlClient;
using Server.Data;
using SharedProject;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Server.Services
{
    public class CallSlipService
    {
        private readonly Dbconnections _dbConnections;
        private readonly ILogger<CallSlipService> _logger;

        public CallSlipService(Dbconnections dbConnections, ILogger<CallSlipService> logger)
        {
            _dbConnections = dbConnections;
            _logger = logger;
        }

        public async Task<int> SaveCallSlipAsync(CallSlipModel callSlip)
        {
            try
            {
                _logger.LogInformation("Saving call slip for IncidentID: {IncidentID}, EscalationID: {EscalationID}, GeneratedBy: {GeneratedBy}", 
                    callSlip.IncidentID, callSlip.EscalationID, callSlip.GeneratedBy);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                
                _logger.LogInformation("Database connection opened successfully");

                var query = @"
                    INSERT INTO callslips (
                        IncidentID, EscalationID, ComplainantName, VictimName, RespondentName,
                        DateReported, MeetingDate, MeetingTime,
                        SchoolName, PODTeacherName, PODPosition,
                        GeneratedBy, GeneratedDate, IsActive
                    ) VALUES (
                        @IncidentID, @EscalationID, @ComplainantName, @VictimName, @RespondentName,
                        @DateReported, @MeetingDate, @MeetingTime,
                        @SchoolName, @PODTeacherName, @PODPosition,
                        @GeneratedBy, @GeneratedDate, @IsActive
                    )";

                using var command = new MySqlCommand(query, connection);
                // Handle nullable IncidentID - if it's 0 or null, set to DBNull
                var incId = (callSlip.IncidentID > 0) ? (object)callSlip.IncidentID : DBNull.Value;
                var escId = (callSlip.EscalationID.HasValue && callSlip.EscalationID.Value > 0) ? (object)callSlip.EscalationID.Value : DBNull.Value;
                
                _logger.LogInformation($"[DEBUG-SAVE] IncidentID: {incId}, EscalationID: {escId}");
                _logger.LogInformation($"[DEBUG-SAVE] Complainant: {callSlip.ComplainantName}, Victim: {callSlip.VictimName}, Respondent: {callSlip.RespondentName}");
                _logger.LogInformation($"[DEBUG-SAVE] School: {callSlip.SchoolName}, Teacher: {callSlip.PODTeacherName}, By: {callSlip.GeneratedBy}");

                command.Parameters.AddWithValue("@IncidentID", incId);
                command.Parameters.AddWithValue("@EscalationID", escId);
                command.Parameters.AddWithValue("@ComplainantName", callSlip.ComplainantName ?? "N/A");
                command.Parameters.AddWithValue("@VictimName", callSlip.VictimName ?? "N/A");
                command.Parameters.AddWithValue("@RespondentName", callSlip.RespondentName ?? "N/A");
                command.Parameters.AddWithValue("@DateReported", callSlip.DateReported);
                command.Parameters.AddWithValue("@MeetingDate", callSlip.MeetingDate.HasValue ? (object)callSlip.MeetingDate.Value : DBNull.Value);
                command.Parameters.AddWithValue("@MeetingTime", callSlip.MeetingTime.HasValue ? (object)callSlip.MeetingTime.Value : DBNull.Value);
                command.Parameters.AddWithValue("@SchoolName", callSlip.SchoolName ?? "Unknown School");
                command.Parameters.AddWithValue("@PODTeacherName", callSlip.PODTeacherName ?? "N/A");
                command.Parameters.AddWithValue("@PODPosition", callSlip.PODPosition ?? "POD");
                command.Parameters.AddWithValue("@GeneratedBy", callSlip.GeneratedBy ?? "System");
                command.Parameters.AddWithValue("@GeneratedDate", DateTime.Now);
                command.Parameters.AddWithValue("@IsActive", 1);

                await command.ExecuteNonQueryAsync();
                var callSlipId = Convert.ToInt32(command.LastInsertedId);

                _logger.LogInformation("Successfully saved call slip with ID: {CallSlipID} for IncidentID: {IncidentID}, EscalationID: {EscalationID}", 
                    callSlipId, callSlip.IncidentID, callSlip.EscalationID);
                return callSlipId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving call slip for IncidentID: {IncidentID}: {Message}. InnerException: {InnerException}", 
                    callSlip.IncidentID, ex.Message, ex.InnerException?.Message);
                
                // Provide more helpful error message if table doesn't exist or column missing
                if (ex.Message.Contains("doesn't exist") || ex.Message.Contains("Table '") || ex.Message.Contains("Unknown table") || ex.Message.Contains("Unknown column"))
                {
                    var errorMsg = $"Database schema error. Please ensure callslips table exists and has EscalationID column. Original error: {ex.Message}";
                    _logger.LogError(errorMsg);
                    throw new Exception(errorMsg, ex);
                }
                
                throw;
            }
        }

        public async Task<List<CallSlipModel>> GetCallSlipsByEscalationIdAsync(int escalationId)
        {
            var callSlips = new List<CallSlipModel>();

            try
            {
                _logger.LogInformation("Retrieving call slips for EscalationID: {EscalationID}", escalationId);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT CallSlipID, IncidentID, EscalationID, ComplainantName, VictimName, RespondentName,
                           DateReported, MeetingDate, MeetingTime,
                           SchoolName, PODTeacherName, PODPosition,
                           GeneratedBy, GeneratedDate, IsActive
                    FROM callslips
                    WHERE EscalationID = @EscalationID AND IsActive = 1
                    ORDER BY GeneratedDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@EscalationID", escalationId);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var callSlip = new CallSlipModel
                    {
                        CallSlipID = reader.GetInt32("CallSlipID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? 0 : reader.GetInt32("IncidentID"),
                        EscalationID = reader.IsDBNull("EscalationID") ? null : reader.GetInt32("EscalationID"),
                        ComplainantName = reader.GetString("ComplainantName"),
                        VictimName = reader.GetString("VictimName"),
                        RespondentName = reader.GetString("RespondentName"),
                        DateReported = reader.GetDateTime("DateReported"),
                        MeetingDate = reader.IsDBNull("MeetingDate") ? null : reader.GetDateTime("MeetingDate"),
                        MeetingTime = reader.IsDBNull("MeetingTime") ? null : ((MySqlDataReader)reader).GetTimeSpan("MeetingTime"),
                        SchoolName = reader.GetString("SchoolName"),
                        PODTeacherName = reader.GetString("PODTeacherName"),
                        PODPosition = reader.GetString("PODPosition"),
                        GeneratedBy = reader.GetString("GeneratedBy"),
                        GeneratedDate = reader.GetDateTime("GeneratedDate"),
                        IsActive = reader.GetInt32("IsActive")
                    };
                    callSlips.Add(callSlip);
                }

                _logger.LogInformation("Retrieved {Count} call slips for EscalationID: {EscalationID}", callSlips.Count, escalationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving call slips for EscalationID: {EscalationID}: {Message}", escalationId, ex.Message);
            }

            return callSlips;
        }

        public async Task<List<CallSlipModel>> GetCallSlipsByIncidentIdAsync(int incidentId)
        {
            var callSlips = new List<CallSlipModel>();

            try
            {
                _logger.LogInformation("Retrieving call slips for IncidentID: {IncidentID}", incidentId);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT CallSlipID, IncidentID, EscalationID, ComplainantName, VictimName, RespondentName,
                           DateReported, MeetingDate, MeetingTime,
                           SchoolName, PODTeacherName, PODPosition,
                           GeneratedBy, GeneratedDate, IsActive
                    FROM callslips
                    WHERE IncidentID = @IncidentID AND IsActive = 1
                    ORDER BY GeneratedDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@IncidentID", incidentId);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var callSlip = new CallSlipModel
                    {
                        CallSlipID = reader.GetInt32("CallSlipID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? 0 : reader.GetInt32("IncidentID"),
                        EscalationID = reader.IsDBNull("EscalationID") ? null : reader.GetInt32("EscalationID"),
                        ComplainantName = reader.GetString("ComplainantName"),
                        VictimName = reader.GetString("VictimName"),
                        RespondentName = reader.GetString("RespondentName"),
                        DateReported = reader.GetDateTime("DateReported"),
                        MeetingDate = reader.IsDBNull("MeetingDate") ? null : reader.GetDateTime("MeetingDate"),
                        MeetingTime = reader.IsDBNull("MeetingTime") ? null : ((MySqlDataReader)reader).GetTimeSpan("MeetingTime"),
                        SchoolName = reader.GetString("SchoolName"),
                        PODTeacherName = reader.GetString("PODTeacherName"),
                        PODPosition = reader.GetString("PODPosition"),
                        GeneratedBy = reader.GetString("GeneratedBy"),
                        GeneratedDate = reader.GetDateTime("GeneratedDate"),
                        IsActive = reader.GetInt32("IsActive")
                    };
                    callSlips.Add(callSlip);
                }

                _logger.LogInformation("Retrieved {Count} call slips for IncidentID: {IncidentID}", callSlips.Count, incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving call slips for IncidentID: {IncidentID}: {Message}", incidentId, ex.Message);
            }

            return callSlips;
        }

        public async Task<List<CallSlipModel>> GetCallSlipsByGeneratedByAsync(string generatedBy)
        {
            var callSlips = new List<CallSlipModel>();

            try
            {
                _logger.LogInformation("Retrieving call slips generated by: {GeneratedBy}", generatedBy);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT CallSlipID, IncidentID, EscalationID, ComplainantName, VictimName, RespondentName,
                           DateReported, MeetingDate, MeetingTime,
                           SchoolName, PODTeacherName, PODPosition,
                           GeneratedBy, GeneratedDate, IsActive
                    FROM callslips
                    WHERE GeneratedBy = @GeneratedBy AND IsActive = 1
                    ORDER BY GeneratedDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@GeneratedBy", generatedBy);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var callSlip = new CallSlipModel
                    {
                        CallSlipID = reader.GetInt32("CallSlipID"),
                        IncidentID = reader.GetInt32("IncidentID"),
                        ComplainantName = reader.GetString("ComplainantName"),
                        VictimName = reader.GetString("VictimName"),
                        RespondentName = reader.GetString("RespondentName"),
                        DateReported = reader.GetDateTime("DateReported"),
                        MeetingDate = reader.IsDBNull("MeetingDate") ? null : reader.GetDateTime("MeetingDate"),
                        MeetingTime = reader.IsDBNull("MeetingTime") ? null : ((MySqlDataReader)reader).GetTimeSpan("MeetingTime"),
                        SchoolName = reader.GetString("SchoolName"),
                        PODTeacherName = reader.GetString("PODTeacherName"),
                        PODPosition = reader.GetString("PODPosition"),
                        GeneratedBy = reader.GetString("GeneratedBy"),
                        GeneratedDate = reader.GetDateTime("GeneratedDate"),
                        IsActive = reader.GetInt32("IsActive")
                    };
                    callSlips.Add(callSlip);
                }

                _logger.LogInformation("Retrieved {Count} call slips generated by: {GeneratedBy}", callSlips.Count, generatedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving call slips by GeneratedBy: {GeneratedBy}: {Message}", generatedBy, ex.Message);
            }

            return callSlips;
        }

        public async Task<(bool TableExists, string Message, string? Error)> TestTableExistsAsync()
        {
            try
            {
                _logger.LogInformation("Testing if callslips table exists");
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if table exists
                var checkQuery = @"
                    SELECT COUNT(*) as TableExists
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'callslips'";

                using var command = new MySqlCommand(checkQuery, connection);
                var result = await command.ExecuteScalarAsync();
                var tableExists = Convert.ToInt32(result) > 0;

                if (tableExists)
                {
                    // Try to get count
                    var countQuery = "SELECT COUNT(*) FROM callslips";
                    using var countCommand = new MySqlCommand(countQuery, connection);
                    var count = await countCommand.ExecuteScalarAsync();
                    
                    return (true, $"Table exists. Current records: {count}", null);
                }
                else
                {
                    return (false, "Table does not exist. Please run the SQL script: Server/Data/callslips_table_schema.sql", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing table existence");
                return (false, "Error checking table", ex.Message);
            }
        }

        public async Task<CallSlipModel?> GetCallSlipByIdAsync(int callSlipId)
        {
            try
            {
                _logger.LogInformation("Retrieving call slip with ID: {CallSlipID}", callSlipId);
                var connectionString = _dbConnections.GetConnection();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT CallSlipID, IncidentID, EscalationID, ComplainantName, VictimName, RespondentName,
                           DateReported, MeetingDate, MeetingTime,
                           SchoolName, PODTeacherName, PODPosition,
                           GeneratedBy, GeneratedDate, IsActive
                    FROM callslips
                    WHERE CallSlipID = @CallSlipID AND IsActive = 1
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@CallSlipID", callSlipId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new CallSlipModel
                    {
                        CallSlipID = reader.GetInt32("CallSlipID"),
                        IncidentID = reader.IsDBNull("IncidentID") ? 0 : reader.GetInt32("IncidentID"),
                        EscalationID = reader.IsDBNull("EscalationID") ? null : reader.GetInt32("EscalationID"),
                        ComplainantName = reader.GetString("ComplainantName"),
                        VictimName = reader.GetString("VictimName"),
                        RespondentName = reader.GetString("RespondentName"),
                        DateReported = reader.GetDateTime("DateReported"),
                        MeetingDate = reader.IsDBNull("MeetingDate") ? null : reader.GetDateTime("MeetingDate"),
                        MeetingTime = reader.IsDBNull("MeetingTime") ? null : ((MySqlDataReader)reader).GetTimeSpan("MeetingTime"),
                        SchoolName = reader.GetString("SchoolName"),
                        PODTeacherName = reader.GetString("PODTeacherName"),
                        PODPosition = reader.GetString("PODPosition"),
                        GeneratedBy = reader.GetString("GeneratedBy"),
                        GeneratedDate = reader.GetDateTime("GeneratedDate"),
                        IsActive = reader.GetInt32("IsActive")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving call slip with ID: {CallSlipID}: {Message}", callSlipId, ex.Message);
                throw;
            }
        }
    }
}

