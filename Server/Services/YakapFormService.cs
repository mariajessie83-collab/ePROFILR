using MySql.Data.MySqlClient;
using SharedProject;
using Microsoft.Extensions.Logging;
using Server.Data;
using System.Data;

namespace Server.Services
{
    public class YakapFormService
    {
        private readonly Dbconnections _dbConnections;
        private readonly ILogger<YakapFormService> _logger;

        public YakapFormService(Dbconnections dbConnections, ILogger<YakapFormService> logger)
        {
            _dbConnections = dbConnections;
            _logger = logger;
        }

        public async Task<YakapFormModel> CreateYakapFormAsync(YakapFormRequest request)
        {
            try
            {
                _logger.LogInformation("Creating Y.A.K.A.P. form for RecordID: {RecordID}, Student: {StudentName}", 
                    request.RecordID, request.StudentName);

                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO yakapforms 
                    (RecordID, StudentName, GradeAndSection, DateOfSession, FacilitatorCounselor, SchoolName, SchoolID,
                     Question1_AnoAngNangyari, Question2_AnoAngIniisipOFeelings, 
                     Question3_AnoAngEpektoSaIba, Question4_AnoAngIniisipTungkolSaDesisyon,
                     Question5_AnoAngGagawinMongIba, Question6_AnongPositibongPagpapahalaga, 
                     Question7_MensaheParaSaHinaharap, Status, CreatedBy, DateCreated, IsActive)
                    VALUES 
                    (@RecordID, @StudentName, @GradeAndSection, @DateOfSession, @FacilitatorCounselor, @SchoolName, @SchoolID,
                     @Question1, @Question2, @Question3, @Question4, @Question5, @Question6, @Question7,
                     'Sent', @CreatedBy, NOW(), 1);
                    SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@RecordID", request.RecordID);
                command.Parameters.AddWithValue("@StudentName", request.StudentName);
                command.Parameters.AddWithValue("@GradeAndSection", request.GradeAndSection);
                command.Parameters.AddWithValue("@DateOfSession", request.DateOfSession);
                command.Parameters.AddWithValue("@FacilitatorCounselor", request.FacilitatorCounselor);
                command.Parameters.AddWithValue("@SchoolName", request.SchoolName);
                command.Parameters.AddWithValue("@SchoolID", request.SchoolID.HasValue ? (object)request.SchoolID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@Question1", request.Question1_AnoAngNangyari);
                command.Parameters.AddWithValue("@Question2", request.Question2_AnoAngIniisipOFeelings);
                command.Parameters.AddWithValue("@Question3", request.Question3_AnoAngEpektoSaIba);
                command.Parameters.AddWithValue("@Question4", request.Question4_AnoAngIniisipTungkolSaDesisyon);
                command.Parameters.AddWithValue("@Question5", request.Question5_AnoAngGagawinMongIba);
                command.Parameters.AddWithValue("@Question6", request.Question6_AnongPositibongPagpapahalaga);
                command.Parameters.AddWithValue("@Question7", request.Question7_MensaheParaSaHinaharap);
                command.Parameters.AddWithValue("@CreatedBy", request.CreatedBy);

                var yakapFormID = Convert.ToInt32(await command.ExecuteScalarAsync());

                _logger.LogInformation("Y.A.K.A.P. form created successfully with ID: {YakapFormID}", yakapFormID);

                // Return the created form
                return await GetYakapFormByIdAsync(yakapFormID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Y.A.K.A.P. form: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<YakapFormModel?> GetYakapFormByIdAsync(int yakapFormId)
        {
            try
            {
                _logger.LogInformation("Getting Y.A.K.A.P. form with ID: {YakapFormID}", yakapFormId);

                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT YakapFormID, RecordID, StudentName, GradeAndSection, DateOfSession, FacilitatorCounselor, SchoolName, SchoolID,
                           Question1_AnoAngNangyari, Question2_AnoAngIniisipOFeelings,
                           Question3_AnoAngEpektoSaIba, Question4_AnoAngIniisipTungkolSaDesisyon,
                           Question5_AnoAngGagawinMongIba, Question6_AnongPositibongPagpapahalaga,
                           Question7_MensaheParaSaHinaharap, Status, DateCreated, DateModified, CreatedBy, ModifiedBy, IsActive
                    FROM yakapforms
                    WHERE YakapFormID = @YakapFormID AND IsActive = 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@YakapFormID", yakapFormId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new YakapFormModel
                    {
                        YakapFormID = reader.GetInt32("YakapFormID"),
                        RecordID = reader.GetInt32("RecordID"),
                        StudentName = reader.GetString("StudentName"),
                        GradeAndSection = reader.GetString("GradeAndSection"),
                        DateOfSession = reader.GetDateTime("DateOfSession"),
                        FacilitatorCounselor = reader.GetString("FacilitatorCounselor"),
                        SchoolName = reader.GetString("SchoolName"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        Question1_AnoAngNangyari = reader.GetString("Question1_AnoAngNangyari"),
                        Question2_AnoAngIniisipOFeelings = reader.GetString("Question2_AnoAngIniisipOFeelings"),
                        Question3_AnoAngEpektoSaIba = reader.GetString("Question3_AnoAngEpektoSaIba"),
                        Question4_AnoAngIniisipTungkolSaDesisyon = reader.GetString("Question4_AnoAngIniisipTungkolSaDesisyon"),
                        Question5_AnoAngGagawinMongIba = reader.GetString("Question5_AnoAngGagawinMongIba"),
                        Question6_AnongPositibongPagpapahalaga = reader.GetString("Question6_AnongPositibongPagpapahalaga"),
                        Question7_MensaheParaSaHinaharap = reader.GetString("Question7_MensaheParaSaHinaharap"),
                        Status = reader.GetString("Status"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        DateModified = reader.IsDBNull("DateModified") ? null : reader.GetDateTime("DateModified"),
                        CreatedBy = reader.GetString("CreatedBy"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetString("ModifiedBy"),
                        IsActive = reader.GetBoolean("IsActive")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Y.A.K.A.P. form: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<YakapFormModel?> GetYakapFormByRecordIdAsync(int recordId)
        {
            try
            {
                _logger.LogInformation("Getting Y.A.K.A.P. form for RecordID: {RecordID}", recordId);

                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT YakapFormID, RecordID, StudentName, GradeAndSection, DateOfSession, FacilitatorCounselor, SchoolName, SchoolID,
                           Question1_AnoAngNangyari, Question2_AnoAngIniisipOFeelings,
                           Question3_AnoAngEpektoSaIba, Question4_AnoAngIniisipTungkolSaDesisyon,
                           Question5_AnoAngGagawinMongIba, Question6_AnongPositibongPagpapahalaga,
                           Question7_MensaheParaSaHinaharap, Status, DateCreated, DateModified, CreatedBy, ModifiedBy, IsActive
                    FROM yakapforms
                    WHERE RecordID = @RecordID AND IsActive = 1
                    ORDER BY DateCreated DESC
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@RecordID", recordId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new YakapFormModel
                    {
                        YakapFormID = reader.GetInt32("YakapFormID"),
                        RecordID = reader.GetInt32("RecordID"),
                        StudentName = reader.GetString("StudentName"),
                        GradeAndSection = reader.GetString("GradeAndSection"),
                        DateOfSession = reader.GetDateTime("DateOfSession"),
                        FacilitatorCounselor = reader.GetString("FacilitatorCounselor"),
                        SchoolName = reader.GetString("SchoolName"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        Question1_AnoAngNangyari = reader.GetString("Question1_AnoAngNangyari"),
                        Question2_AnoAngIniisipOFeelings = reader.GetString("Question2_AnoAngIniisipOFeelings"),
                        Question3_AnoAngEpektoSaIba = reader.GetString("Question3_AnoAngEpektoSaIba"),
                        Question4_AnoAngIniisipTungkolSaDesisyon = reader.GetString("Question4_AnoAngIniisipTungkolSaDesisyon"),
                        Question5_AnoAngGagawinMongIba = reader.GetString("Question5_AnoAngGagawinMongIba"),
                        Question6_AnongPositibongPagpapahalaga = reader.GetString("Question6_AnongPositibongPagpapahalaga"),
                        Question7_MensaheParaSaHinaharap = reader.GetString("Question7_MensaheParaSaHinaharap"),
                        Status = reader.GetString("Status"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        DateModified = reader.IsDBNull("DateModified") ? null : reader.GetDateTime("DateModified"),
                        CreatedBy = reader.GetString("CreatedBy"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetString("ModifiedBy"),
                        IsActive = reader.GetBoolean("IsActive")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Y.A.K.A.P. form by RecordID: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<YakapFormModel>> GetAllYakapFormsAsync(int? recordId = null)
        {
            var forms = new List<YakapFormModel>();

            try
            {
                _logger.LogInformation("Getting all Y.A.K.A.P. forms");

                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT YakapFormID, RecordID, StudentName, GradeAndSection, DateOfSession, FacilitatorCounselor, SchoolName, SchoolID,
                           Question1_AnoAngNangyari, Question2_AnoAngIniisipOFeelings,
                           Question3_AnoAngEpektoSaIba, Question4_AnoAngIniisipTungkolSaDesisyon,
                           Question5_AnoAngGagawinMongIba, Question6_AnongPositibongPagpapahalaga,
                           Question7_MensaheParaSaHinaharap, Status, DateCreated, DateModified, CreatedBy, ModifiedBy, IsActive
                    FROM yakapforms
                    WHERE IsActive = 1";

                if (recordId.HasValue)
                {
                    query += " AND RecordID = @RecordID";
                }

                query += " ORDER BY DateCreated DESC";

                using var command = new MySqlCommand(query, connection);
                
                if (recordId.HasValue)
                {
                    command.Parameters.AddWithValue("@RecordID", recordId.Value);
                }

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    forms.Add(new YakapFormModel
                    {
                        YakapFormID = reader.GetInt32("YakapFormID"),
                        RecordID = reader.GetInt32("RecordID"),
                        StudentName = reader.GetString("StudentName"),
                        GradeAndSection = reader.GetString("GradeAndSection"),
                        DateOfSession = reader.GetDateTime("DateOfSession"),
                        FacilitatorCounselor = reader.GetString("FacilitatorCounselor"),
                        SchoolName = reader.GetString("SchoolName"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        Question1_AnoAngNangyari = reader.GetString("Question1_AnoAngNangyari"),
                        Question2_AnoAngIniisipOFeelings = reader.GetString("Question2_AnoAngIniisipOFeelings"),
                        Question3_AnoAngEpektoSaIba = reader.GetString("Question3_AnoAngEpektoSaIba"),
                        Question4_AnoAngIniisipTungkolSaDesisyon = reader.GetString("Question4_AnoAngIniisipTungkolSaDesisyon"),
                        Question5_AnoAngGagawinMongIba = reader.GetString("Question5_AnoAngGagawinMongIba"),
                        Question6_AnongPositibongPagpapahalaga = reader.GetString("Question6_AnongPositibongPagpapahalaga"),
                        Question7_MensaheParaSaHinaharap = reader.GetString("Question7_MensaheParaSaHinaharap"),
                        Status = reader.GetString("Status"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        DateModified = reader.IsDBNull("DateModified") ? null : reader.GetDateTime("DateModified"),
                        CreatedBy = reader.GetString("CreatedBy"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetString("ModifiedBy"),
                        IsActive = reader.GetBoolean("IsActive")
                    });
                }

                _logger.LogInformation("Found {Count} Y.A.K.A.P. forms", forms.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Y.A.K.A.P. forms: {Message}", ex.Message);
                throw;
            }

            return forms;
        }

        public async Task<List<YakapFormModel>> GetYakapFormsByUsernameAsync(string username)
        {
            var forms = new List<YakapFormModel>();

            try
            {
                _logger.LogInformation("Getting Y.A.K.A.P. forms for username: {Username}", username);

                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Join with Students and Users tables to match by username
                var query = @"
                    SELECT yf.YakapFormID, yf.RecordID, yf.StudentName, yf.GradeAndSection, yf.DateOfSession, 
                           yf.FacilitatorCounselor, yf.SchoolName, yf.SchoolID,
                           yf.Question1_AnoAngNangyari, yf.Question2_AnoAngIniisipOFeelings,
                           yf.Question3_AnoAngEpektoSaIba, yf.Question4_AnoAngIniisipTungkolSaDesisyon,
                           yf.Question5_AnoAngGagawinMongIba, yf.Question6_AnongPositibongPagpapahalaga,
                           yf.Question7_MensaheParaSaHinaharap, yf.Status, 
                           yf.DateCreated, yf.DateModified, yf.CreatedBy, yf.ModifiedBy, yf.IsActive
                    FROM yakapforms yf
                    INNER JOIN studentprofilecaserecords cr ON yf.RecordID = cr.RecordID
                    INNER JOIN students s ON 
                        (
                            UPPER(TRIM(cr.StudentOffenderName)) = UPPER(TRIM(s.StudentName))
                            OR UPPER(TRIM(cr.StudentOffenderName)) LIKE CONCAT('%', UPPER(TRIM(s.StudentName)), '%')
                            OR UPPER(TRIM(s.StudentName)) LIKE CONCAT('%', UPPER(TRIM(cr.StudentOffenderName)), '%')
                        )
                    INNER JOIN users u ON s.UserID = u.UserID
                    WHERE u.Username = @Username
                      AND s.IsActive = 1
                      AND (yf.IsActive = 1 OR yf.IsActive IS NULL)
                    ORDER BY yf.DateCreated DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username.Trim());

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    forms.Add(new YakapFormModel
                    {
                        YakapFormID = reader.GetInt32("YakapFormID"),
                        RecordID = reader.GetInt32("RecordID"),
                        StudentName = reader.GetString("StudentName"),
                        GradeAndSection = reader.GetString("GradeAndSection"),
                        DateOfSession = reader.GetDateTime("DateOfSession"),
                        FacilitatorCounselor = reader.GetString("FacilitatorCounselor"),
                        SchoolName = reader.GetString("SchoolName"),
                        SchoolID = reader.IsDBNull("SchoolID") ? null : reader.GetInt32("SchoolID"),
                        Question1_AnoAngNangyari = reader.GetString("Question1_AnoAngNangyari"),
                        Question2_AnoAngIniisipOFeelings = reader.GetString("Question2_AnoAngIniisipOFeelings"),
                        Question3_AnoAngEpektoSaIba = reader.GetString("Question3_AnoAngEpektoSaIba"),
                        Question4_AnoAngIniisipTungkolSaDesisyon = reader.GetString("Question4_AnoAngIniisipTungkolSaDesisyon"),
                        Question5_AnoAngGagawinMongIba = reader.GetString("Question5_AnoAngGagawinMongIba"),
                        Question6_AnongPositibongPagpapahalaga = reader.GetString("Question6_AnongPositibongPagpapahalaga"),
                        Question7_MensaheParaSaHinaharap = reader.GetString("Question7_MensaheParaSaHinaharap"),
                        Status = reader.GetString("Status"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        DateModified = reader.IsDBNull("DateModified") ? null : reader.GetDateTime("DateModified"),
                        CreatedBy = reader.GetString("CreatedBy"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetString("ModifiedBy"),
                        IsActive = reader.GetBoolean("IsActive")
                    });
                }

                _logger.LogInformation("Found {Count} Y.A.K.A.P. forms for username: {Username}", forms.Count, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Y.A.K.A.P. forms by username: {Message}", ex.Message);
                throw;
            }

            return forms;
        }

        public async Task<YakapFormModel?> UpdateYakapFormAsync(int yakapFormId, YakapFormRequest request)
        {
            try
            {
                _logger.LogInformation("Updating Y.A.K.A.P. form {YakapFormID} for RecordID: {RecordID}", yakapFormId, request.RecordID);

                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Determine status based on answers
                string status = "Sent";
                if (!string.IsNullOrWhiteSpace(request.Question1_AnoAngNangyari) &&
                    !string.IsNullOrWhiteSpace(request.Question2_AnoAngIniisipOFeelings) &&
                    !string.IsNullOrWhiteSpace(request.Question3_AnoAngEpektoSaIba) &&
                    !string.IsNullOrWhiteSpace(request.Question4_AnoAngIniisipTungkolSaDesisyon) &&
                    !string.IsNullOrWhiteSpace(request.Question5_AnoAngGagawinMongIba) &&
                    !string.IsNullOrWhiteSpace(request.Question6_AnongPositibongPagpapahalaga) &&
                    !string.IsNullOrWhiteSpace(request.Question7_MensaheParaSaHinaharap))
                {
                    status = "Completed";
                }
                else if (!string.IsNullOrWhiteSpace(request.Question1_AnoAngNangyari) ||
                         !string.IsNullOrWhiteSpace(request.Question2_AnoAngIniisipOFeelings) ||
                         !string.IsNullOrWhiteSpace(request.Question3_AnoAngEpektoSaIba) ||
                         !string.IsNullOrWhiteSpace(request.Question4_AnoAngIniisipTungkolSaDesisyon) ||
                         !string.IsNullOrWhiteSpace(request.Question5_AnoAngGagawinMongIba) ||
                         !string.IsNullOrWhiteSpace(request.Question6_AnongPositibongPagpapahalaga) ||
                         !string.IsNullOrWhiteSpace(request.Question7_MensaheParaSaHinaharap))
                {
                    status = "InProgress";
                }

                var query = @"
                    UPDATE yakapforms 
                    SET StudentName = @StudentName,
                        GradeAndSection = @GradeAndSection,
                        DateOfSession = @DateOfSession,
                        FacilitatorCounselor = @FacilitatorCounselor,
                        SchoolName = @SchoolName,
                        SchoolID = @SchoolID,
                        Question1_AnoAngNangyari = @Question1,
                        Question2_AnoAngIniisipOFeelings = @Question2,
                        Question3_AnoAngEpektoSaIba = @Question3,
                        Question4_AnoAngIniisipTungkolSaDesisyon = @Question4,
                        Question5_AnoAngGagawinMongIba = @Question5,
                        Question6_AnongPositibongPagpapahalaga = @Question6,
                        Question7_MensaheParaSaHinaharap = @Question7,
                        Status = @Status,
                        DateModified = NOW(),
                        ModifiedBy = @CreatedBy
                    WHERE YakapFormID = @YakapFormID AND IsActive = 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@YakapFormID", yakapFormId);
                command.Parameters.AddWithValue("@StudentName", request.StudentName);
                command.Parameters.AddWithValue("@GradeAndSection", request.GradeAndSection);
                command.Parameters.AddWithValue("@DateOfSession", request.DateOfSession);
                command.Parameters.AddWithValue("@FacilitatorCounselor", request.FacilitatorCounselor);
                command.Parameters.AddWithValue("@SchoolName", request.SchoolName);
                command.Parameters.AddWithValue("@SchoolID", request.SchoolID.HasValue ? (object)request.SchoolID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@Question1", request.Question1_AnoAngNangyari);
                command.Parameters.AddWithValue("@Question2", request.Question2_AnoAngIniisipOFeelings);
                command.Parameters.AddWithValue("@Question3", request.Question3_AnoAngEpektoSaIba);
                command.Parameters.AddWithValue("@Question4", request.Question4_AnoAngIniisipTungkolSaDesisyon);
                command.Parameters.AddWithValue("@Question5", request.Question5_AnoAngGagawinMongIba);
                command.Parameters.AddWithValue("@Question6", request.Question6_AnongPositibongPagpapahalaga);
                command.Parameters.AddWithValue("@Question7", request.Question7_MensaheParaSaHinaharap);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@CreatedBy", request.CreatedBy);

                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Y.A.K.A.P. form updated successfully with Status: {Status}", status);
                    return await GetYakapFormByIdAsync(yakapFormId);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Y.A.K.A.P. form: {Message}", ex.Message);
                throw;
            }
        }
    }
}

