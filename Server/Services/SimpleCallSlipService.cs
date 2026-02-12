using System.Text;
using SharedProject;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace Server.Services
{
    public class SimpleCallSlipService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public SimpleCallSlipService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public Task<byte[]> GenerateCallSlipPdfAsync(int incidentId)
        {
            try
            {
                // Generate HTML with sample data for now
                var html = GenerateCallSlipHtml(incidentId);

                // Return HTML as bytes
                return Task.FromResult(Encoding.UTF8.GetBytes(html));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating call slip: {ex.Message}", ex);
            }
        }

        private string GenerateCallSlipHtml(int incidentId)
        {
            // Sample data for demonstration
            string complainantName = "Juan Dela Cruz";
            string victimName = "Maria Santos";
            string respondentName = "Pedro Garcia";
            string schoolName = "Kauswagan National High School - Senior High School";
            string gradeLevel = "Grade 10";
            string section = "Section A";
            string roomNo = "Room 101";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Call Slip - Incident ID: {incidentId}</title>
    <style>
        body {{ font-family: 'Arial', sans-serif; margin: 0; padding: 20px; font-size: 12pt; }}
        .container {{ width: 8.5in; height: 11in; margin: 0 auto; border: 1px solid #ccc; padding: 30px; box-sizing: border-box; }}
        .deped-header {{ text-align: center; margin-bottom: 10px; }}
        .deped-header strong {{ font-size: 10pt; display: block; }}
        .header {{ text-align: center; margin-bottom: 20px; }}
        .title {{ font-size: 24pt; font-weight: bold; margin-bottom: 5px; }}
        .school-name {{ font-size: 14pt; font-weight: bold; margin-bottom: 20px; }}
        .form-section {{ margin-bottom: 20px; }}
        .form-row {{ display: flex; justify-content: space-between; margin-bottom: 10px; }}
        .form-label {{ font-weight: bold; }}
        .form-field {{ border-bottom: 1px solid #000; padding: 0 5px; min-width: 150px; display: inline-block; }}
        .message {{ margin-bottom: 20px; line-height: 1.5; }}
        .names-section {{ margin-bottom: 30px; }}
        .name-field {{ margin-left: 20px; margin-bottom: 5px; border-bottom: 1px solid #000; padding: 0 5px; }}
        .footer {{ text-align: center; margin-top: 50px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='deped-header'>
            <strong>KAGAWAN NG EDUKASYON</strong><br>
            <strong>REPUBLIKA NG PILIPINAS</strong>
        </div>

        <div class='header'>
            <div class='title'>CALL SLIP</div>
            <div class='school-name'>{schoolName}</div>
        </div>

        <div class='form-section'>
            <div class='form-row'>
                <span class='form-label'>DATE:</span>
                <span class='form-field'>{DateTime.Now:MMM dd, yyyy}</span>
                <span class='form-label'>TIME:</span>
                <span class='form-field'>{DateTime.Now:hh:mm tt}</span>
            </div>
            <div class='form-row'>
                <span class='form-label'>VENUE:</span>
                <span class='form-field'>POD Office</span>
            </div>
        </div>

        <div class='message'>
            Please see the POD In-charge for conference.
        </div>

        <div class='form-section'>
            <div class='form-row'>
                <span class='form-label'>Grade Level/ Section:</span>
                <span class='form-field'>{gradeLevel}/{section}</span>
                <span class='form-label' style='margin-left: 50px;'>Room No.:</span>
                <span class='form-field'>{roomNo}</span>
            </div>
        </div>

        <div class='names-section'>
            <div class='form-label'>Name/s:</div>
            <div class='name-field'>{complainantName} (Complainant)</div>
            <div class='name-field'>{victimName} (Victim)</div>
            <div class='name-field'>{respondentName} (Respondent)</div>
            <div class='name-field'>_________________</div>
        </div>

        <div class='footer'>
            Prefect of Discipline In- Charge
        </div>
    </div>
</body>
</html>";
        }
    }
}
