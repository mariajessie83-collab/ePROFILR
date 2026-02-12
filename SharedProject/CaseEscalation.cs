namespace SharedProject
{
    public class CaseEscalation
    {
        public int EscalationID { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string? TrackStrand { get; set; } // Added
        public int? SchoolID { get; set; }
        public string? SchoolName { get; set; } // Added
        public int MinorCaseCount { get; set; }
        public string? CaseDetails { get; set; } // Added
        public string EscalatedBy { get; set; } = string.Empty;
        public int? TeacherID { get; set; }
        public DateTime EscalatedDate { get; set; }
        public EscalationStatus Status { get; set; } = EscalationStatus.Active;
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class EscalateStudentRequest
    {
        public string StudentName { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string? TrackStrand { get; set; } // Added
        public string? SchoolName { get; set; } // Added
        public int MinorCaseCount { get; set; }
        public string? CaseDetails { get; set; } // Added
        public string? Notes { get; set; }
    }
}
