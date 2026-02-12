using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace SharedProject
{
    public class Studentclass
    {
        public int StudentID { get; set; }
        public int UserID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string StudentName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;
        
        public DateTime DateRegister { get; set; } = DateTime.Now;
        
        [StringLength(50)]
        public string? Section { get; set; }
        
        [StringLength(255)]
        public string? Address { get; set; }
        
        public DateTime? BirthDate { get; set; }
        
        [StringLength(20)]
        public string? GradeLevel { get; set; }
        
        [StringLength(20)]
        public string? SchoolYear { get; set; }
        
        [StringLength(100)]
        public string? ParentName { get; set; }
        
        [StringLength(20)]
        public string? ParentContact { get; set; }
        
        // New fields for school information
        public int? SchoolID { get; set; }
        
        [StringLength(200)]
        public string? SchoolName { get; set; }
        
        [StringLength(20)]
        public string? School_ID { get; set; }
        
        [StringLength(50)]
        public string? Strand { get; set; }
        
        // Teacher relationship
        public int? TeacherID { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [StringLength(50)]
        public string? Username { get; set; }
        
        [StringLength(255)]
        public string? Password { get; set; }

        // Guardian Information
        [StringLength(100)]
        public string? FathersName { get; set; }
        
        [StringLength(100)]
        public string? MothersName { get; set; }
        
        [StringLength(100)]
        public string? GuardianName { get; set; }
        
        [StringLength(20)]
        public string? GuardianContact { get; set; }
        
        [StringLength(20)]
        public string? ContactPerson { get; set; }
    }

    public class StudentRegistrationRequest
    {
        [Required]
        [StringLength(100)]
        public string StudentName { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? Username { get; set; }
        
        [StringLength(100)]
        public string? Password { get; set; }
        
        [StringLength(100)]
        public string? ConfirmPassword { get; set; }
        
        [Required]
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;
        
        public int? Age { get; set; }
        
        [StringLength(50)]
        public string? Section { get; set; }
        
        [StringLength(255)]
        public string? Address { get; set; }
        
        public DateTime? BirthDate { get; set; }
        
        [StringLength(20)]
        public string? GradeLevel { get; set; }
        
        [StringLength(20)]
        public string? SchoolYear { get; set; }
        
        [StringLength(100)]
        public string? ParentName { get; set; }
        
        [StringLength(20)]
        public string? ParentContact { get; set; }
        
        [StringLength(20)]
        public string? EmergencyContact { get; set; }
        
        // New fields for school information
        public int? SchoolID { get; set; }
        
        [StringLength(200)]
        public string? SchoolName { get; set; }
        
        [StringLength(20)]
        public string? School_ID { get; set; }
        
        [StringLength(50)]
        public string? Strand { get; set; }
        
        // Guardian Information
        [StringLength(100)]
        public string? FathersName { get; set; }
        
        [StringLength(100)]
        public string? MothersName { get; set; }
        
        [StringLength(100)]
        public string? GuardianName { get; set; }
        
        [StringLength(20)]
        public string? GuardianContact { get; set; }
        
        [StringLength(20)]
        public string? ContactPerson { get; set; } // "Father", "Mother", "Guardian", "Both"

        // Teacher relationship
        public int? TeacherID { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
