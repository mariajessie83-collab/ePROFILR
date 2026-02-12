using System.ComponentModel.DataAnnotations;

namespace SharedProject
{
    public class YakapFormModel
    {
        public int YakapFormID { get; set; }
        
        [Required(ErrorMessage = "Record ID is required")]
        public int RecordID { get; set; }
        
        [Required(ErrorMessage = "Student name is required")]
        [StringLength(200, ErrorMessage = "Student name cannot exceed 200 characters")]
        public string StudentName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Grade and section is required")]
        [StringLength(100, ErrorMessage = "Grade and section cannot exceed 100 characters")]
        public string GradeAndSection { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Date of session is required")]
        public DateTime DateOfSession { get; set; } = DateTime.Now;
        
        [Required(ErrorMessage = "Facilitator/Counselor is required")]
        [StringLength(200, ErrorMessage = "Facilitator/Counselor cannot exceed 200 characters")]
        public string FacilitatorCounselor { get; set; } = string.Empty;
        
        // School Information
        [Required(ErrorMessage = "School is required")]
        [StringLength(200, ErrorMessage = "School name cannot exceed 200 characters")]
        public string SchoolName { get; set; } = string.Empty;
        
        public int? SchoolID { get; set; }
        
        // Bahagi I – Pag-unawa sa Aking Karanasan
        [Required(ErrorMessage = "Question 1 (Ano ang nangyari?) is required")]
        public string Question1_AnoAngNangyari { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Question 2 (Ano ang iniisip o nararamdaman mo?) is required")]
        public string Question2_AnoAngIniisipOFeelings { get; set; } = string.Empty;
        
        // Bahagi II – Pananagutan sa Ginawang Pagkilos
        [Required(ErrorMessage = "Question 3 (Ano ang naging epekto sa iba?) is required")]
        public string Question3_AnoAngEpektoSaIba { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Question 4 (Ano ang iniisip mo tungkol sa desisyon?) is required")]
        public string Question4_AnoAngIniisipTungkolSaDesisyon { get; set; } = string.Empty;
        
        // Bahagi III – Pagharap sa Hinaharap Gamit ang Positibong Disiplina
        [Required(ErrorMessage = "Question 5 (Ano ang gagawin mong iba?) is required")]
        public string Question5_AnoAngGagawinMongIba { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Question 6 (Anong positibong pagpapahalaga?) is required")]
        public string Question6_AnongPositibongPagpapahalaga { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Question 7 (Mensahe para sa hinaharap na sarili) is required")]
        public string Question7_MensaheParaSaHinaharap { get; set; } = string.Empty;
        
        // Status: Sent, InProgress, Completed
        public string Status { get; set; } = "Sent";
        
        // Metadata
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateModified { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class YakapFormRequest
    {
        public int RecordID { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string GradeAndSection { get; set; } = string.Empty;
        public DateTime DateOfSession { get; set; } = DateTime.Now;
        public string FacilitatorCounselor { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public int? SchoolID { get; set; }
        public string Question1_AnoAngNangyari { get; set; } = string.Empty;
        public string Question2_AnoAngIniisipOFeelings { get; set; } = string.Empty;
        public string Question3_AnoAngEpektoSaIba { get; set; } = string.Empty;
        public string Question4_AnoAngIniisipTungkolSaDesisyon { get; set; } = string.Empty;
        public string Question5_AnoAngGagawinMongIba { get; set; } = string.Empty;
        public string Question6_AnongPositibongPagpapahalaga { get; set; } = string.Empty;
        public string Question7_MensaheParaSaHinaharap { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }
}

