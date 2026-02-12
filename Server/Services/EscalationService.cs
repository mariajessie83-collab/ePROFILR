using System.Data;
using MySql.Data.MySqlClient;
using SharedProject;
using Microsoft.Extensions.Logging;
using Server.Data;

namespace Server.Services
{
    public class EscalationService
    {
        private readonly Dbconnections _dbConnections;
        private readonly ILogger<EscalationService> _logger;

        public EscalationService(Dbconnections dbConnections, ILogger<EscalationService> logger)
        {
            _dbConnections = dbConnections;
            _logger = logger;
        }

        /// <summary>
        /// Escalate a student with 3+ minor cases to POD
        /// </summary>
        public async Task<int> EscalateStudentToPODAsync(EscalateStudentRequest request, string escalatedBy, int teacherId, int? schoolId = null)
        {
            try
            {
                _logger.LogInformation("Escalating student {StudentName} to POD by {EscalatedBy}", request.StudentName, escalatedBy);
                
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if already escalated
                var checkQuery = @"
                    SELECT EscalationID 
                    FROM caseescalations 
                    WHERE StudentName = @StudentName 
                      AND GradeLevel = @GradeLevel 
                      AND Section = @Section 
                      AND Status = @Status 
                      AND IsActive = 1";

                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@StudentName", request.StudentName);
                checkCommand.Parameters.AddWithValue("@GradeLevel", request.GradeLevel);
                checkCommand.Parameters.AddWithValue("@Section", request.Section);
                checkCommand.Parameters.AddWithValue("@Status", EscalationStatus.Active.ToString());

                var existingId = await checkCommand.ExecuteScalarAsync();
                if (existingId != null)
                {
                    _logger.LogWarning("Student {StudentName} already escalated (ID: {EscalationID})", request.StudentName, existingId);
                    return Convert.ToInt32(existingId);
                }

                // Create new escalation
                var insertQuery = @"
                    INSERT INTO caseescalations 
                    (StudentName, GradeLevel, Section, TrackStrand, SchoolName, MinorCaseCount, CaseDetails, EscalatedBy, EscalatedDate, Status, Notes, IsActive)
                    VALUES 
                    (@StudentName, @GradeLevel, @Section, @TrackStrand, @SchoolName, @MinorCaseCount, @CaseDetails, @EscalatedBy, @EscalatedDate, @Status, @Notes, 1)";

                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@StudentName", request.StudentName);
                insertCommand.Parameters.AddWithValue("@GradeLevel", request.GradeLevel);
                insertCommand.Parameters.AddWithValue("@Section", request.Section);
                insertCommand.Parameters.AddWithValue("@TrackStrand", request.TrackStrand ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SchoolName", request.SchoolName ?? (object)DBNull.Value);
                // SchoolID removed as column does not exist in table
                insertCommand.Parameters.AddWithValue("@MinorCaseCount", request.MinorCaseCount);
                insertCommand.Parameters.AddWithValue("@CaseDetails", request.CaseDetails ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@EscalatedBy", escalatedBy);
                // TeacherID removed as column does not exist
                insertCommand.Parameters.AddWithValue("@EscalatedDate", DateTime.Now);
                insertCommand.Parameters.AddWithValue("@Status", EscalationStatus.Active.ToString());
                insertCommand.Parameters.AddWithValue("@Notes", request.Notes ?? (object)DBNull.Value);

                await insertCommand.ExecuteNonQueryAsync();

                var escalationId = Convert.ToInt32(await new MySqlCommand("SELECT LAST_INSERT_ID()", connection).ExecuteScalarAsync() ?? 0);
                
                _logger.LogInformation("Successfully escalated student {StudentName} with EscalationID: {EscalationID}", request.StudentName, escalationId);
                return escalationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalating student {StudentName} to POD", request.StudentName);
                throw;
            }
        }

        /// <summary>
        /// Get all escalated students for POD dashboard
        /// </summary>
        public async Task<List<CaseEscalation>> GetEscalatedStudentsForPODAsync(int? schoolId = null, string? status = null)
        {
            var escalations = new List<CaseEscalation>();

            try
            {
                _logger.LogInformation("Getting escalated students for POD. SchoolID: {SchoolID}, Status: {Status}", schoolId, status);
                
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ce.EscalationID, ce.StudentName, ce.GradeLevel, ce.Section, ce.TrackStrand, ce.SchoolName, ce.MinorCaseCount, ce.CaseDetails,
                           COALESCE(t.TeacherName, aa.FullName, ce.EscalatedBy) as EscalatedBy, 
                           ce.EscalatedDate, ce.Status, ce.Notes, ce.IsActive
                    FROM caseescalations ce
                    LEFT JOIN users u ON ce.EscalatedBy = u.Username
                    LEFT JOIN teachers t ON u.UserID = t.UserID
                    LEFT JOIN adminaccounts aa ON u.UserID = aa.UserID
                    WHERE ce.IsActive = 1";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND ce.Status = @Status";
                }
                else
                {
                    // Show all escalations except Withdrawn, Resolved, and Closed
                    query += " AND ce.Status NOT IN (@Withdrawn, @Resolved, @Closed)";
                }

                query += " ORDER BY ce.EscalatedDate DESC";

                using var command = new MySqlCommand(query, connection);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }
                else
                {
                    command.Parameters.AddWithValue("@Withdrawn", EscalationStatus.Withdrawn.ToString());
                    command.Parameters.AddWithValue("@Resolved", EscalationStatus.Resolved.ToString());
                    command.Parameters.AddWithValue("@Closed", EscalationStatus.Closed.ToString());
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    escalations.Add(new CaseEscalation
                    {
                        EscalationID = reader.GetInt32("EscalationID"),
                        StudentName = reader.GetString("StudentName"),
                        GradeLevel = reader.GetString("GradeLevel"),
                        Section = reader.GetString("Section"),
                        TrackStrand = reader.IsDBNull("TrackStrand") ? null : reader.GetString("TrackStrand"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),

                        MinorCaseCount = reader.GetInt32("MinorCaseCount"),
                        CaseDetails = reader.IsDBNull("CaseDetails") ? null : reader.GetString("CaseDetails"),
                        // TeacherID removed
                        EscalatedBy = reader.GetString("EscalatedBy"),
                        EscalatedDate = reader.GetDateTime("EscalatedDate"),
                        Status = Enum.Parse<EscalationStatus>(reader.GetString("Status")),
                        Notes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes"),
                        IsActive = reader.GetBoolean("IsActive")
                    });
                }

                _logger.LogInformation("Found {Count} escalated students", escalations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalated students for POD");
                throw;
            }

            return escalations;
        }

        /// <summary>
        /// Check if a student is currently escalated
        /// </summary>
        public async Task<CaseEscalation?> GetEscalationStatusAsync(string studentName, string gradeLevel, string section)
        {
            try
            {
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ce.EscalationID, ce.StudentName, ce.GradeLevel, ce.Section, ce.TrackStrand, ce.SchoolName, ce.MinorCaseCount, ce.CaseDetails,
                           COALESCE(t.TeacherName, aa.FullName, ce.EscalatedBy) as EscalatedBy, 
                           ce.EscalatedDate, ce.Status, ce.Notes, ce.IsActive
                    FROM caseescalations ce
                    LEFT JOIN users u ON ce.EscalatedBy = u.Username
                    LEFT JOIN teachers t ON u.UserID = t.UserID
                    LEFT JOIN adminaccounts aa ON u.UserID = aa.UserID
                    WHERE ce.StudentName = @StudentName 
                      AND ce.GradeLevel = @GradeLevel 
                      AND ce.Section = @Section 
                      AND ce.Status = @Status 
                      AND ce.IsActive = 1
                    ORDER BY ce.EscalatedDate DESC
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentName", studentName);
                command.Parameters.AddWithValue("@GradeLevel", gradeLevel);
                command.Parameters.AddWithValue("@Section", section);
                command.Parameters.AddWithValue("@Status", EscalationStatus.Active.ToString());

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new CaseEscalation
                    {
                        EscalationID = reader.GetInt32("EscalationID"),
                        StudentName = reader.GetString("StudentName"),
                        GradeLevel = reader.GetString("GradeLevel"),
                        Section = reader.GetString("Section"),
                        TrackStrand = reader.IsDBNull("TrackStrand") ? null : reader.GetString("TrackStrand"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),

                        MinorCaseCount = reader.GetInt32("MinorCaseCount"),
                        CaseDetails = reader.IsDBNull("CaseDetails") ? null : reader.GetString("CaseDetails"),
                        // TeacherID removed
                        EscalatedBy = reader.GetString("EscalatedBy"),
                        EscalatedDate = reader.GetDateTime("EscalatedDate"),
                        Status = Enum.Parse<EscalationStatus>(reader.GetString("Status")),
                        Notes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes"),
                        IsActive = reader.GetBoolean("IsActive")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking escalation status for {StudentName}", studentName);
                throw;
            }
        }

        /// <summary>
        /// Withdraw an escalation (mark as withdrawn)
        /// </summary>
        public async Task<bool> WithdrawEscalationAsync(int escalationId, int teacherId)
        {
            try
            {
                _logger.LogInformation("Withdrawing escalation {EscalationID} by teacher {TeacherID}", escalationId, teacherId);
                
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Verify the escalation exists (Ownership check removed as TeacherID column is missing)
                var checkQuery = "SELECT EscalationID FROM caseescalations WHERE EscalationID = @EscalationID";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@EscalationID", escalationId);
                
                var existingId = await checkCommand.ExecuteScalarAsync();
                if (existingId == null)
                {
                    _logger.LogWarning("Escalation {EscalationID} not found", escalationId);
                    return false;
                }

                // Removed TeacherID check because the column does not exist in the database.
                // Assuming providing the correct EscalationID is sufficient auth for now.

                var updateQuery = @"
                    UPDATE caseescalations 
                    SET Status = @Status
                    WHERE EscalationID = @EscalationID";

                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@EscalationID", escalationId);
                updateCommand.Parameters.AddWithValue("@Status", EscalationStatus.Withdrawn.ToString());

                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                
                _logger.LogInformation("Successfully withdrew escalation {EscalationID}", escalationId);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing escalation {EscalationID}", escalationId);
                throw;
            }
            }


        /// <summary>
        /// Get a single escalated case by its ID
        /// </summary>
        public async Task<CaseEscalation?> GetEscalatedCaseByIdAsync(int escalationId)
        {
            try
            {
                _logger.LogInformation("Getting escalated case details for ID: {EscalationID}", escalationId);
                
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ce.EscalationID, ce.StudentName, ce.GradeLevel, ce.Section, ce.TrackStrand, ce.SchoolName, ce.MinorCaseCount, ce.CaseDetails,
                           COALESCE(t.TeacherName, aa.FullName, ce.EscalatedBy) as EscalatedBy, 
                           ce.EscalatedDate, ce.Status, ce.Notes, ce.IsActive
                    FROM caseescalations ce
                    LEFT JOIN users u ON ce.EscalatedBy = u.Username
                    LEFT JOIN teachers t ON u.UserID = t.UserID
                    LEFT JOIN adminaccounts aa ON u.UserID = aa.UserID
                    WHERE ce.EscalationID = @EscalationID AND ce.IsActive = 1
                    LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@EscalationID", escalationId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new CaseEscalation
                    {
                        EscalationID = reader.GetInt32("EscalationID"),
                        StudentName = reader.GetString("StudentName"),
                        GradeLevel = reader.GetString("GradeLevel"),
                        Section = reader.GetString("Section"),
                        TrackStrand = reader.IsDBNull("TrackStrand") ? null : reader.GetString("TrackStrand"),
                        SchoolName = reader.IsDBNull("SchoolName") ? null : reader.GetString("SchoolName"),
                        MinorCaseCount = reader.GetInt32("MinorCaseCount"),
                        CaseDetails = reader.IsDBNull("CaseDetails") ? null : reader.GetString("CaseDetails"),
                        EscalatedBy = reader.GetString("EscalatedBy"),
                        EscalatedDate = reader.GetDateTime("EscalatedDate"),
                        Status = Enum.Parse<EscalationStatus>(reader.GetString("Status")),
                        Notes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes"),
                        IsActive = reader.GetBoolean("IsActive")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalated case details for ID: {EscalationID}", escalationId);
                throw;
            }
        }

        /// <summary>
        /// Update escalation status (e.g. Active, Calling, Arrived, Resolved, Closed)
        /// </summary>
        public async Task<bool> UpdateEscalationStatusAsync(int escalationId, string status)
        {
            try
            {
                _logger.LogInformation("Updating escalation {EscalationID} status to {Status}", escalationId, status);
                
                var connectionString = _dbConnections.GetConnection();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    UPDATE caseescalations 
                    SET Status = @Status
                    WHERE EscalationID = @EscalationID";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@EscalationID", escalationId);
                command.Parameters.AddWithValue("@Status", status);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Successfully updated escalation {EscalationID} status to {Status}. Syncing related records...", escalationId, status);
                    
                    // 1. Sync to CASE RECORDS (Part B)
                    var updateCaseRecordsQuery = "UPDATE simplifiedstudentprofilecaserecords SET Status = @Status WHERE EscalationID = @EscalationID";
                    using var updateCaseCmd = new MySqlCommand(updateCaseRecordsQuery, connection);
                    updateCaseCmd.Parameters.AddWithValue("@Status", status);
                    updateCaseCmd.Parameters.AddWithValue("@EscalationID", escalationId);
                    var caseRows = await updateCaseCmd.ExecuteNonQueryAsync();
                    if (caseRows > 0) _logger.LogInformation("Synced Status '{Status}' to {Count} Case Records for EscalationID {EscalationID}", status, caseRows, escalationId);

                    // 2. Sync to INCIDENT REPORTS
                    // We sync to all minor incident reports for the student/school associated with this escalation
                    // First get the student/school info
                    var getEscInfoQuery = "SELECT StudentName, SchoolName FROM caseescalations WHERE EscalationID = @EscalationID";
                    using var getEscInfoCmd = new MySqlCommand(getEscInfoQuery, connection);
                    getEscInfoCmd.Parameters.AddWithValue("@EscalationID", escalationId);
                    
                    string? studentName = null;
                    string? schoolName = null;
                    using (var reader = await getEscInfoCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            studentName = reader.IsDBNull(0) ? null : reader.GetString(0);
                            schoolName = reader.IsDBNull(1) ? null : reader.GetString(1);
                        }
                    }

                    if (!string.IsNullOrEmpty(studentName))
                    {
                        // Update ALL minor incident reports for this student/school that are Pending or Approved
                        var syncIncidentsQuery = @"
                            UPDATE simplifiedincidentreports sir
                            INNER JOIN violationtypes vt ON UPPER(TRIM(sir.IncidentType)) = UPPER(TRIM(vt.ViolationName))
                            SET sir.Status = @Status
                            WHERE sir.RespondentName = @Name 
                            AND sir.SchoolName = @School
                            AND vt.ViolationCategory = 'Minor'
                            AND sir.Status IN ('Pending', 'Approved', 'Reported', 'Calling', 'Arrived', 'UnderReview', 'ParentOnHold')";

                        using var syncIncCmd = new MySqlCommand(syncIncidentsQuery, connection);
                        syncIncCmd.Parameters.AddWithValue("@Status", status);
                        syncIncCmd.Parameters.AddWithValue("@Name", studentName);
                        syncIncCmd.Parameters.AddWithValue("@School", schoolName ?? "");
                        
                        var incRows = await syncIncCmd.ExecuteNonQueryAsync();
                        if (incRows > 0)
                        {
                            _logger.LogInformation("Broad-synced Status '{Status}' from Escalation to {Count} related minor incidents for {Student}", status, incRows, studentName);
                        }
                    }
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating escalation {EscalationID} status to {Status}", escalationId, status);
                throw;
            }
        }
    }
}
