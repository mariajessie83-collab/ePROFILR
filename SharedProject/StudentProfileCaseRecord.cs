using System.ComponentModel.DataAnnotations;

namespace SharedProject
{
    public class StudentProfileCaseRecordModel
    {
        public int? IncidentID { get; set; }

        [Required(ErrorMessage = "Student offender name is required")]
        [StringLength(100, ErrorMessage = "Student offender name cannot exceed 100 characters")]
        public string StudentOffenderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Grade level is required")]
        [StringLength(20, ErrorMessage = "Grade level cannot exceed 20 characters")]
        public string GradeLevel { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Track/Strand cannot exceed 50 characters")]
        public string TrackStrand { get; set; } = string.Empty;

        [Required(ErrorMessage = "Section is required")]
        [StringLength(50, ErrorMessage = "Section cannot exceed 50 characters")]
        public string Section { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adviser name is required")]
        [StringLength(100, ErrorMessage = "Adviser name cannot exceed 100 characters")]
        public string AdviserName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Father's name cannot exceed 100 characters")]
        public string? FathersName { get; set; }

        [StringLength(100, ErrorMessage = "Mother's name cannot exceed 100 characters")]
        public string? MothersName { get; set; }

        [StringLength(100, ErrorMessage = "Guardian's name cannot exceed 100 characters")]
        public string? GuardianName { get; set; }

        [Required(ErrorMessage = "Parent/Guardian contact number is required")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters")]
        public string ParentGuardianContact { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of offense is required")]
        public DateTime DateOfOffense { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Level of offense is required")]
        public string LevelOfOffense { get; set; } = string.Empty;

        [Required(ErrorMessage = "Violation committed is required")]
        public string ViolationCommitted { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Other violation description cannot exceed 500 characters")]
        public string? OtherViolationDescription { get; set; }

        [Required(ErrorMessage = "Number of offense is required")]
        public string NumberOfOffense { get; set; } = string.Empty;

        [Required(ErrorMessage = "Details of agreement is required")]
        [StringLength(1000, ErrorMessage = "Details of agreement cannot exceed 1000 characters")]
        public string DetailsOfAgreement { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "POD In-Charge name cannot exceed 100 characters")]
        public string? PODInCharge { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Active";
        public string? SchoolName { get; set; }
        public string? Region { get; set; }
        public string? Division { get; set; }
        public string? District { get; set; }

        // Photo and signature fields
        public string? EvidencePhotoBase64 { get; set; }
        public string? SignatureBase64 { get; set; }
    }

    public class StudentProfileCaseRecordRequest
    {
        public int? IncidentID { get; set; }
        public string StudentOffenderName { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public string TrackStrand { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string AdviserName { get; set; } = string.Empty;
        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        public string? GuardianName { get; set; }
        public string ParentGuardianContact { get; set; } = string.Empty;
        public DateTime DateOfOffense { get; set; } = DateTime.Now;
        public string LevelOfOffense { get; set; } = string.Empty;
        public string ViolationCommitted { get; set; } = string.Empty;
        public string? OtherViolationDescription { get; set; }
        public string NumberOfOffense { get; set; } = string.Empty;
        public string DetailsOfAgreement { get; set; } = string.Empty;
        public string? PODInCharge { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Active";

        // Photo and signature fields
        public string? EvidencePhotoBase64 { get; set; }
        public string? SignatureBase64 { get; set; }
    }

    public class TeacherSearchResult
    {
        public int TeacherID { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string GradeHandle { get; set; } = string.Empty;
        public string SectionHandle { get; set; } = string.Empty;
        // Optional SHS track/strand (e.g., STEM, HUMSS, TVL-ATTRACTION)
        public string TrackStrand { get; set; } = string.Empty;
        public string? SchoolName { get; set; }
    }

    public class StudentProfileCaseRecordSummary
    {
        public int RecordID { get; set; }
        public int? IncidentID { get; set; }
        public string StudentOffenderName { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string ViolationCommitted { get; set; } = string.Empty;
        public string LevelOfOffense { get; set; } = string.Empty;
        public DateTime DateOfOffense { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DetailsOfAgreement { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public string NumberOfOffense { get; set; } = string.Empty;
        public string AdviserName { get; set; } = string.Empty;
        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        public string? GuardianName { get; set; }
        public string? SchoolName { get; set; }
        public string? Region { get; set; }
        public string? Division { get; set; }
        public string? District { get; set; }
        public string? EvidencePhotoBase64 { get; set; }
        public string? SignatureBase64 { get; set; }
        public string? ParentGuardianContact { get; set; }
        // Guidance Escalation Fields
        public bool IsSentToGuidance { get; set; }
        public DateTime? DateSentToGuidance { get; set; }
        public string? GuidanceCounselorName { get; set; }
        public string? ReferredBy { get; set; }
    }

    public class SimplifiedStudentProfileCaseRecordModel
    {
        public int RecordID { get; set; }
        public int? IncidentID { get; set; }
        public int? EscalationID { get; set; }
        public string RespondentName { get; set; } = string.Empty;
        public DateTime DateOfOffense { get; set; } = DateTime.Now;
        public string ViolationCommitted { get; set; } = string.Empty;
        public string? ViolationCategory { get; set; }
        public string? Description { get; set; }
        public string? EvidencePhotoBase64 { get; set; }
        public string? StudentSignatureBase64 { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string? Sex { get; set; }
        public string? Address { get; set; }
        public string? GradeLevel { get; set; }
        public string? TrackStrand { get; set; }
        public string? Section { get; set; }
        public string? AdviserName { get; set; }
        public string? ActionTaken { get; set; }
        public string? Findings { get; set; }
        public string? Agreement { get; set; }
        public string? PenaltyAction { get; set; }
        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        public string? GuardianName { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        // New: parent/guardian details for major classification
        public string? ParentContactType { get; set; } // "Father", "Mother", "Guardian"
        public string? ParentContactName { get; set; }
        public DateTime? ParentMeetingDate { get; set; }
        
        // School information (joined from schools table)
        public string? SchoolName { get; set; }
        public string? Region { get; set; }
        public string? Division { get; set; }
        public string? District { get; set; }
        // Guidance Escalation Fields
        public bool IsSentToGuidance { get; set; }
        public DateTime? DateSentToGuidance { get; set; }
        public string? GuidanceCounselorName { get; set; }
        public string? ReferredBy { get; set; }

        // New: Victim and Complainant details from incident report
        public string? VictimName { get; set; }
        public string? ComplainantName { get; set; }
        public string? ComplainantContact { get; set; }
        public string? VictimContact { get; set; }
    }


    public class SimplifiedStudentProfileCaseRecordRequest
    {
        public int? IncidentID { get; set; }
        public int? EscalationID { get; set; }
        public string RespondentName { get; set; } = string.Empty;
        public DateTime DateOfOffense { get; set; } = DateTime.Now;
        public string ViolationCommitted { get; set; } = string.Empty;
        public string? ViolationCategory { get; set; }
        public string? Description { get; set; }
        public string? EvidencePhotoBase64 { get; set; }
        public string? StudentSignatureBase64 { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string? Sex { get; set; }
        public string? Address { get; set; }
        public string? GradeLevel { get; set; }
        public string? TrackStrand { get; set; }
        public string? Section { get; set; }
        public string? AdviserName { get; set; }
        public string? ActionTaken { get; set; }
        public string? Findings { get; set; }
        public string? Agreement { get; set; }
        public string? PenaltyAction { get; set; }
        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        public string? GuardianName { get; set; }
        public string Status { get; set; } = "Active";
        // New: parent/guardian details for classification phase
        public string? ParentContactType { get; set; }
        public string? ParentContactName { get; set; }
        public DateTime? ParentMeetingDate { get; set; }

        // New: Victim and Complainant details
        public string? VictimName { get; set; }
        public string? ComplainantName { get; set; }
        public string? ComplainantContact { get; set; }
        public string? VictimContact { get; set; }
    }

    public class ParentMeetingUpdateRequest
    {
        public string? ParentContactType { get; set; }
        public string? ParentContactName { get; set; }
        public DateTime? ParentMeetingDate { get; set; }
        public string? Status { get; set; }
    }

    public class PartBUpdateRequest
    {
        public string? ActionTaken { get; set; }
        public string? Findings { get; set; }
        public string? Agreement { get; set; }
        public string? PenaltyAction { get; set; }
        public string? Status { get; set; }
    }
}