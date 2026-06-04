using System.ComponentModel.DataAnnotations;

namespace LeaveService.Models;

/// <summary>
/// Request DTO for applying leave
/// </summary>
public class ApplyLeaveRequest
{
    [Required(ErrorMessage = "Leave type is required")]
    public string LeaveType { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }
    
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }
    
    [Required(ErrorMessage = "Reason is required")]
    [MinLength(10, ErrorMessage = "Reason must be at least 10 characters")]
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; set; } = string.Empty;
}
