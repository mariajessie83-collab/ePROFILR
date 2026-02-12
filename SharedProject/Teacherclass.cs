using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SharedProject
{
    public class Teacherclass
    {
        public int TeacherID { get; set; }
        public int UserID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TeacherName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        public DateTime DateRegister { get; set; } = DateTime.Now;
        
        [Required]
        [StringLength(50)]
        public string Position { get; set; } = string.Empty;
        
        [StringLength(10)]
        public string? Gender { get; set; }
        
        // New fields for school information
        public int? SchoolID { get; set; }
        
        [StringLength(200)]
        public string? SchoolName { get; set; }
        
        [StringLength(20)]
        public string? School_ID { get; set; }
        
        // New fields for grade level, section, and strand
        [StringLength(20)]
        public string? GradeLevel { get; set; }
        
        [StringLength(50)]
        public string? Section { get; set; }
        
        [StringLength(50)]
        public string? Strand { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
    
    public class User
    {
        public int UserID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string UserRole { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }
    }
    
    public class TeacherRegistrationRequest
    {
        [Required]
        [StringLength(100)]
        public string TeacherName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Position { get; set; } = string.Empty;
        
        [StringLength(10)]
        public string? Gender { get; set; }
        
        // New fields for school information
        public int? SchoolID { get; set; }
        
        [StringLength(200)]
        public string? SchoolName { get; set; }
        
        [StringLength(20)]
        public string? School_ID { get; set; }
        
        // New fields for grade level, section, and strand
        [StringLength(20)]
        public string? GradeLevel { get; set; }
        
        [StringLength(50)]
        public string? Section { get; set; }
        
        [StringLength(50)]
        public string? Strand { get; set; }
    }
    
    
    public class LoginRequest
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;
    }
    
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")]
        public T? Data { get; set; }
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();
    }
    
    // New class for school data (simplified)
    public class School
    {
        public int? SchoolID { get; set; }
        public string School_ID { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string? Region { get; set; }
        public string? Division { get; set; }
        public string? District { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime DateCreated { get; set; } = DateTime.Now;
        
        // Additional properties for dropdown functionality
        public int RegionID { get; set; }
        public string RegionName { get; set; } = string.Empty;
        public int DivisionID { get; set; }
        public string DivisionName { get; set; } = string.Empty;
        public int DistrictID { get; set; }
        public string DistrictName { get; set; } = string.Empty;
    }
}
