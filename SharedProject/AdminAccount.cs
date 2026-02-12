using System;
using System.ComponentModel.DataAnnotations;

namespace SharedProject
{
    public class AdminAccount
    {
        public int AdminAccountID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        [StringLength(20)]
        public string AccountType { get; set; } = string.Empty; // admin, schoolhead, division
        
        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;
        
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        // School Head specific fields
        public int? SchoolID { get; set; }
        
        [StringLength(200)]
        public string? SchoolName { get; set; }
        
        [StringLength(50)]
        public string? School_ID { get; set; }
        
        [StringLength(100)]
        public string? Division { get; set; }
        
        [StringLength(100)]
        public string? Region { get; set; }
        
        [StringLength(100)]
        public string? District { get; set; }
        
        // Division specific fields
        [StringLength(100)]
        public string? DivisionName { get; set; }
        
        // Common fields
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }
        
        // Navigation/display properties
        public string? Username { get; set; }
        public string? CreatorName { get; set; }
        public string? UserPassword { get; set; }
    }
    
    public class AdminAccountRequest
    {
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string AccountType { get; set; } = string.Empty; // admin, schoolhead, division
        
        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;
        
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        // School Head specific fields
        public int? SchoolID { get; set; }
        
        [StringLength(200)]
        public string? SchoolName { get; set; }
        
        [StringLength(50)]
        public string? School_ID { get; set; }
        
        [StringLength(100)]
        public string? Division { get; set; }
        
        [StringLength(100)]
        public string? Region { get; set; }
        
        [StringLength(100)]
        public string? District { get; set; }
        
        // Division specific fields
        [StringLength(100)]
        public string? DivisionName { get; set; }
    }
    
    public class AdminAccountUpdateRequest
    {
        public int AdminAccountID { get; set; }
        
        [StringLength(200)]
        public string? FullName { get; set; }
        
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        // School Head specific fields
        public int? SchoolID { get; set; }
        
        [StringLength(200)]
        public string? SchoolName { get; set; }
        
        [StringLength(50)]
        public string? School_ID { get; set; }
        
        [StringLength(100)]
        public string? Division { get; set; }
        
        [StringLength(100)]
        public string? Region { get; set; }
        
        [StringLength(100)]
        public string? District { get; set; }
        
        // Division specific fields
        [StringLength(100)]
        public string? DivisionName { get; set; }
        
        public bool? IsActive { get; set; }
    }
}

