using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Server.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using SharedProject;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Server.Services
{
    public class CallSlipReportService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly ILogger<CallSlipReportService> _logger;

        public CallSlipReportService(IConfiguration configuration, ILogger<CallSlipReportService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("dbconstring") ?? "";
        }

        public async Task<CallSlipModel?> GetCallSlipModelAsync(int incidentId, DateTime? meetingDate = null, TimeSpan? meetingTime = null, string? generatedBy = null)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string query = @"
                        SELECT 
                            ir.IncidentId,
                            ir.FullName as ComplainantName,
                            COALESCE(ir.VictimName, 'N/A') as VictimName,
                            ir.RespondentName,
                            ir.DateReported,
                            COALESCE(ir.SchoolName, 'Unknown School') AS SchoolName,
                            @GeneratedBy AS TeacherName,
                            'POD' AS Position
                        FROM SimplifiedIncidentReports ir
                        WHERE ir.IncidentId = @IncidentId
                        LIMIT 1";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IncidentId", incidentId);
                        command.Parameters.AddWithValue("@GeneratedBy", generatedBy ?? "POD In-Charge");
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new CallSlipModel
                                {
                                    IncidentID = reader.GetInt32("IncidentId"),
                                    ComplainantName = reader.GetString("ComplainantName"),
                                    VictimName = reader.GetString("VictimName"),
                                    RespondentName = reader.GetString("RespondentName"),
                                    DateReported = reader.GetDateTime("DateReported"),
                                    MeetingDate = meetingDate,
                                    MeetingTime = meetingTime,
                                    SchoolName = reader.GetString("SchoolName"),
                                    PODTeacherName = reader.GetString("TeacherName"),
                                    PODPosition = reader.GetString("Position"),
                                    GeneratedDate = DateTime.Now
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving call slip model: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<CallSlipModel?> GetEscalationCallSlipModelAsync(int escalationId, DateTime? meetingDate = null, TimeSpan? meetingTime = null, string? generatedBy = null)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string query = @"
                        SELECT 
                            ce.EscalationID,
                            ce.EscalatedBy as ComplainantName,
                            'N/A' as VictimName,
                            ce.StudentName as RespondentName,
                            ce.EscalatedDate as DateReported,
                            COALESCE(ce.SchoolName, 'Unknown School') AS SchoolName,
                            @GeneratedBy AS TeacherName,
                            'POD' AS Position
                        FROM CaseEscalations ce
                        WHERE ce.EscalationID = @EscalationID
                        LIMIT 1";

                    _logger.LogInformation($"Executing query for EscalationID: {escalationId}");

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EscalationID", escalationId);
                        command.Parameters.AddWithValue("@GeneratedBy", generatedBy ?? "POD In-Charge");
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new CallSlipModel
                                {
                                    IncidentID = 0, // No incident ID for escalations
                                    EscalationID = reader.GetInt32("EscalationID"),
                                    ComplainantName = reader.GetString("ComplainantName"),
                                    VictimName = reader.GetString("VictimName"),
                                    RespondentName = reader.GetString("RespondentName"),
                                    DateReported = reader.GetDateTime("DateReported"),
                                    MeetingDate = meetingDate,
                                    MeetingTime = meetingTime,
                                    SchoolName = reader.GetString("SchoolName"),
                                    PODTeacherName = reader.GetString("TeacherName"),
                                    PODPosition = reader.GetString("Position"),
                                    GeneratedDate = DateTime.Now
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving escalation call slip model: {ex.Message}", ex);
            }

            return null;
        }

        // Kept for backward compatibility if needed, but primarily we use the Model-based approach now
        public async Task<DataTable> GetCallSlipDataAsync(int incidentId, string? generatedBy = null)
        {
            var dataTable = new DataTable();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string query = @"
                        SELECT 
                            ir.IncidentId,
                            ir.FullName as ComplainantName,
                            COALESCE(ir.VictimName, 'N/A') as VictimName,
                            ir.RespondentName,
                            ir.DateReported,
                            NOW() as TimeReported,
                            COALESCE(ir.SchoolName, 'Unknown School') AS SchoolName,
                            'N/A' AS GradeLevel,
                            'N/A' AS Section,
                            'N/A' AS RoomNumber,
                            COALESCE(ir.Status, 'Pending') AS Status,
                            @GeneratedBy AS TeacherName,
                            'POD' AS Position
                        FROM SimplifiedIncidentReports ir
                        WHERE ir.IncidentId = @IncidentId
                        LIMIT 1";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IncidentId", incidentId);
                        command.Parameters.AddWithValue("@GeneratedBy", generatedBy ?? "POD In-Charge");
                        command.CommandTimeout = 10;
                        
                        using (var adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving call slip data: {ex.Message}", ex);
            }

            return dataTable;
        }

        public async Task<DataTable> GetEscalationCallSlipDataAsync(int escalationId, string? generatedBy = null)
        {
            var dataTable = new DataTable();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string query = @"
                        SELECT 
                            ce.EscalationID as IncidentId,
                            ce.EscalatedBy as ComplainantName,
                            'N/A' as VictimName,
                            ce.StudentName as RespondentName,
                            ce.EscalatedDate as DateReported,
                            NOW() as TimeReported,
                            COALESCE(ce.SchoolName, 'Unknown School') AS SchoolName,
                            COALESCE(ce.GradeLevel, 'N/A') as GradeLevel,
                            COALESCE(ce.Section, 'N/A') as Section,
                            'N/A' as RoomNumber,
                            ce.Status,
                            @GeneratedBy AS TeacherName,
                            'POD' AS Position
                        FROM CaseEscalations ce
                        WHERE ce.EscalationID = @EscalationID
                        LIMIT 1";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EscalationID", escalationId);
                        command.Parameters.AddWithValue("@GeneratedBy", generatedBy ?? "POD In-Charge");
                        command.CommandTimeout = 10;
                        
                        using (var adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving escalation call slip data: {ex.Message}", ex);
            }

            return dataTable;
        }

        public async Task<byte[]> GenerateCallSlipReportAsync(int incidentId, DateTime? meetingDate = null, TimeSpan? meetingTime = null, string? generatedBy = null)
        {
            try
            {
                var model = await GetCallSlipModelAsync(incidentId, meetingDate, meetingTime, generatedBy);
                
                if (model == null)
                {
                    throw new Exception($"No data found for incident ID: {incidentId}");
                }

                return RenderQuestPdf(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating call slip report: {ex.Message}");
                throw;
            }
        }

        public async Task<byte[]> GenerateEscalationCallSlipReportAsync(int escalationId, DateTime? meetingDate = null, TimeSpan? meetingTime = null, string? generatedBy = null)
        {
            try
            {
                var model = await GetEscalationCallSlipModelAsync(escalationId, meetingDate, meetingTime, generatedBy);
                
                if (model == null)
                {
                    throw new Exception($"No data found for escalation ID: {escalationId}");
                }

                return RenderQuestPdf(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating escalation call slip report: {ex.Message}");
                throw;
            }
        }

        private byte[] RenderQuestPdf(CallSlipModel model)
        {
            // QuestPDF License is set in Program.cs

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Portrait());
                    page.Margin(2, Unit.Centimetre); // Standard formal letter margin
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Times New Roman")); // Serif font for formal look

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);

                        // Header
                        column.Item().AlignCenter().Column(header =>
                        {
                            header.Spacing(3);
                            header.Item().AlignCenter().Text("Republic of the Philippines").FontSize(10);
                            header.Item().AlignCenter().Text("Department of Education").FontSize(10).Bold();
                            header.Item().AlignCenter().Text("Region XII").FontSize(10);
                            header.Item().AlignCenter().Text("Division of City Schools").FontSize(10);
                            
                            // School Name (Larger, from database)
                            header.Item().PaddingTop(8).AlignCenter().Text(model.SchoolName.ToUpper()).FontSize(14).Bold().FontFamily("Arial");
                            
                            header.Item().PaddingTop(3).AlignCenter().Text("Office of the Prefect of Discipline").FontSize(11).Italic();
                        });

                        column.Item().PaddingTop(20).LineHorizontal(1);

                        // Date
                        column.Item().PaddingTop(20).AlignRight().Text(DateTime.Now.ToString("MMMM dd, yyyy"));

                        // Title
                        column.Item().PaddingTop(20).AlignCenter().Text("CALL SLIP").FontSize(16).Bold().Underline();

                        // Body
                        column.Item().PaddingTop(30).Column(body =>
                        {
                            body.Spacing(10);
                            
                            body.Item().Text($"To: {model.RespondentName}").Bold().FontSize(12);
                            
                            body.Item().PaddingTop(10).Text("Greetings!").FontSize(12);

                            body.Item().Text(text =>
                            {
                                text.Span("This is to request your presence at the ");
                                text.Span("Office of the Prefect of Discipline").Bold();
                                text.Span(" for a conference regarding a reported incident.");
                            });

                            // Schedule Details (Formal list, no boxes)
                            body.Item().PaddingTop(15).PaddingLeft(20).Column(details =>
                            {
                                details.Spacing(5);
                                
                                details.Item().Row(row =>
                                {
                                    row.ConstantItem(100).Text("Date:").Bold();
                                    row.RelativeItem().Text(model.MeetingDate?.ToString("MMMM dd, yyyy") ?? "As soon as possible");
                                });

                                details.Item().Row(row =>
                                {
                                    row.ConstantItem(100).Text("Time:").Bold();
                                    row.RelativeItem().Text(model.MeetingTime.HasValue ? DateTime.Today.Add(model.MeetingTime.Value).ToString("hh:mm tt") : "During break time");
                                });
                                
                                details.Item().Row(row =>
                                {
                                    row.ConstantItem(100).Text("Venue:").Bold();
                                    row.RelativeItem().Text("POD Office");
                                });
                            });

                            body.Item().PaddingTop(15).Text(text =>
                            {
                                text.Span("Your cooperation in this matter is highly appreciated to resolve the issue constructively. ");
                                text.Span("Please bring your parent or guardian if necessary.").Italic();
                            });
                        });

                        // Footer / Signatory
                        column.Item().PaddingTop(50).Column(footer =>
                        {
                            footer.Spacing(2);
                            
                            footer.Item().AlignRight().Column(sig =>
                            {
                                sig.Item().Text("Respectfully yours,");
                                
                                sig.Item().PaddingTop(30).Text(model.PODTeacherName.ToUpper()).Bold().Underline();
                                sig.Item().Text(model.PODPosition).Italic();
                            });
                        });
                        
                        // Reference Number (Small at bottom)
                        column.Item().PaddingTop(40).AlignLeft().Text(text => 
                        {
                            text.Span("Ref No: ").FontSize(8).FontColor(Colors.Grey.Medium);
                            text.Span($"{(model.IncidentID > 0 ? "INC-" + model.IncidentID : "ESC-" + model.EscalationID)}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                    
                    // Footer Page Number
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
