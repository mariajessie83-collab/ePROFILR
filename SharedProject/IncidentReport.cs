using System.ComponentModel.DataAnnotations;

namespace SharedProject
{
    public class IncidentReportModel
    {
        public int IncidentID { get; set; }
        public string? ReferenceNumber { get; set; }
        [Required(ErrorMessage = "Complainant name is required")]
        [StringLength(100, ErrorMessage = "Complainant name cannot exceed 100 characters")]
        public string ComplainantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Grade level is required")]
        [StringLength(20, ErrorMessage = "Grade level cannot exceed 20 characters")]
        public string ComplainantGrade { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Strand cannot exceed 50 characters")]
        public string? ComplainantStrand { get; set; }

        [Required(ErrorMessage = "Section is required")]
        [StringLength(50, ErrorMessage = "Section cannot exceed 50 characters")]
        public string ComplainantSection { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Contact number must be 11 digits starting with 09")]
        public string? ComplainantContactNumber { get; set; }

        [StringLength(100, ErrorMessage = "Victim name cannot exceed 100 characters")]
        public string VictimName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Room number cannot exceed 20 characters")]
        public string? RoomNumber { get; set; }

        [Required(ErrorMessage = "Victim contact is required")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters")]
        public string VictimContact { get; set; } = string.Empty;

        [Required(ErrorMessage = "Incident type is required")]
        public string IncidentType { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Other incident type cannot exceed 100 characters")]
        public string? OtherIncidentType { get; set; }

        [Required(ErrorMessage = "Incident description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string IncidentDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Respondent name is required")]
        [StringLength(500, ErrorMessage = "Respondent name cannot exceed 500 characters")]
        public string RespondentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adviser name is required")]
        [StringLength(500, ErrorMessage = "Adviser name cannot exceed 500 characters")]
        public string AdviserName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "POD incharge name cannot exceed 100 characters")]
        public string? PODIncharge { get; set; }

        public string? EvidencePhotoPath { get; set; }
        public string? EvidencePhotoBase64 { get; set; }

        public DateTime DateReported { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";

        // Location fields
        public string SchoolName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
    }

    public class IncidentReportRequest
    {
        public string ComplainantName { get; set; } = string.Empty;
        public string ComplainantGrade { get; set; } = string.Empty;
        public string? ComplainantStrand { get; set; }
        public string ComplainantSection { get; set; } = string.Empty;
        public string VictimName { get; set; } = string.Empty;
        public string? RoomNumber { get; set; }
        public string VictimContact { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string? OtherIncidentType { get; set; }
        public string IncidentDescription { get; set; } = string.Empty;
        public string RespondentName { get; set; } = string.Empty;
        public string AdviserName { get; set; } = string.Empty;
        public string? PODIncharge { get; set; }
        public string? EvidencePhotoPath { get; set; }
        public string? EvidencePhotoBase64 { get; set; }
        public DateTime DateReported { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";

        // Location fields - should be populated from user's actual location
        public string SchoolName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
    }

    public class PODTeacher
    {
        public int TeacherID { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string GradeHandle { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string SchoolID { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
    }

    public class IncidentReportSummary
    {
        public int IncidentID { get; set; }
        public string ComplainantName { get; set; } = string.Empty;
        public string VictimName { get; set; } = string.Empty;
        public string RespondentName { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string? OtherIncidentType { get; set; }
        public string IncidentDescription { get; set; } = string.Empty;
        public DateTime DateReported { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string LevelOfOffense { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public string? EvidencePhotoBase64 { get; set; }
        // Guidance Escalation Fields
        public bool IsSentToGuidance { get; set; }
        public DateTime? DateSentToGuidance { get; set; }
        public string? GuidanceCounselorName { get; set; }
        public string? ReferredBy { get; set; }
    }

    public class CallSlipModel
    {
        public int CallSlipID { get; set; }
        public int IncidentID { get; set; }
        public string ComplainantName { get; set; } = string.Empty;
        public string VictimName { get; set; } = string.Empty;
        public string RespondentName { get; set; } = string.Empty;
        public DateTime DateReported { get; set; }
        public DateTime? MeetingDate { get; set; }
        public TimeSpan? MeetingTime { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public string PODTeacherName { get; set; } = string.Empty;
        public string PODPosition { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public int IsActive { get; set; } = 1;
        public int? EscalationID { get; set; }
    }

    public class SimplifiedIncidentReportRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? ComplainantContactNumber { get; set; }
        public string RespondentName { get; set; } = string.Empty;
        public string AdviserName { get; set; } = string.Empty;
        public string? VictimName { get; set; }
        public string IncidentType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EvidencePhotoBase64 { get; set; }
        public string Status { get; set; } = "Pending";
        public string SchoolName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public int? ReporterID { get; set; }
        public string? ReporterRole { get; set; }
    }

    // Simplified Incident Report Model - No student master list dependency
    public class SimplifiedIncidentReport
    {
        public int IncidentID { get; set; }
        public int? ViolationID { get; set; }
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(200, ErrorMessage = "Full name cannot exceed 200 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Contact number must be 11 digits starting with 09")]
        public string? ComplainantContactNumber { get; set; }

        [Required(ErrorMessage = "Respondent name is required")]
        [StringLength(500, ErrorMessage = "Respondent name cannot exceed 500 characters")]
        public string RespondentName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Adviser name cannot exceed 500 characters")]
        public string AdviserName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Victim name cannot exceed 500 characters")]
        public string? VictimName { get; set; }

        [Required(ErrorMessage = "Incident type is required")]
        [StringLength(100, ErrorMessage = "Incident type cannot exceed 100 characters")]
        public string IncidentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        public string? EvidencePhoto { get; set; }
        public string? EvidencePhotoBase64 { get; set; }
        public string? ReferenceNumber { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string Status { get; set; } = "Pending"; // Default: Pending, Approved, Calling, Resolved, Rejected

        // Location fields
        public string SchoolName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public DateTime DateReported { get; set; } = DateTime.Now;
        // Guidance Escalation Fields
        public bool IsSentToGuidance { get; set; }
        public DateTime? DateSentToGuidance { get; set; }
        public string? GuidanceCounselorName { get; set; }
        public string? ReferredBy { get; set; }
        public bool IsActive { get; set; } = true;
        public int? ReporterID { get; set; }
        public string? ReporterRole { get; set; }

        // Helper properties for UI
        public List<string> RespondentNamesList => string.IsNullOrWhiteSpace(RespondentName)
            ? new List<string>() : RespondentName.Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries).ToList();

        public List<string> VictimNamesList => string.IsNullOrWhiteSpace(VictimName)
            ? new List<string>() : VictimName.Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public class OfficialIncidentReport
    {
        public int OfficialIncidentID { get; set; }
        public int ReporterID { get; set; }
        public string ReporterRole { get; set; } = string.Empty; // 'Teacher' or 'POD'

        [Required(ErrorMessage = "Respondent name is required")]
        public string RespondentName { get; set; } = string.Empty;

        public string AdviserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Incident type is required")]
        public string IncidentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public DateTime DateReported { get; set; } = DateTime.Now;
        public string SchoolName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Status { get; set; } = "Reported";
        public string? ReferenceNumber { get; set; }
        public bool IsActive { get; set; } = true;
       

        // Reporter name for UI display
        public string? ReporterName { get; set; }
    }

    public class OfficialIncidentReportRequest
    {
        public int ReporterID { get; set; }
        public string ReporterRole { get; set; } = string.Empty;
        public string RespondentName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Adviser name is required")]
        public string AdviserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Incident type is required")]
        public string IncidentType { get; set; } = string.Empty;

        public string? VictimName { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public string SchoolName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
    }

    public class OfficialIncidentReportSummary
        {
            public int ReporterID { get; set; }
            public string ReporterName { get; set; } = string.Empty;
            public string ReporterRole { get; set; } = string.Empty;
            public int TotalReports { get; set; }
            public DateTime LatestReportDate { get; set; }
            public string SchoolName { get; set; } = string.Empty;
        }

    public class ViolationType
    {
        public int ViolationID { get; set; }
        public string ViolationName { get; set; } = string.Empty;
        public string ViolationCategory { get; set; } = string.Empty;
        public string HasVictim { get; set; } = "Yes";
        public bool IsActive { get; set; } = true;
    }

    public class StudentWithCasesModel
    {
        public string StudentName { get; set; } = string.Empty;
        public string AdviserName { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public int MinorCases { get; set; }
        public int MajorCases { get; set; }
        public int ProhibitedCases { get; set; }
        public int ActiveCases { get; set; }
        public DateTime LatestIncidentDate { get; set; }
    }

    /// <summary>Same as StudentWithCasesModel plus the actual case records (violations) from SimplifiedIncidentReports — one source for count and list.</summary>
    public class StudentWithCasesAndDetailsModel
    {
        public string StudentName { get; set; } = string.Empty;
        public string AdviserName { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public int MinorCases { get; set; }
        public int MajorCases { get; set; }
        public int ProhibitedCases { get; set; }
        public int ActiveCases { get; set; }
        public DateTime LatestIncidentDate { get; set; }
        public List<StudentCaseRecordDto> Cases { get; set; } = new List<StudentCaseRecordDto>();
    }

    public class StudentCaseRecordDto
    {
        public DateTime DateOfOffense { get; set; }
        public string ViolationCommitted { get; set; } = string.Empty;
        public string LevelOfOffense { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}