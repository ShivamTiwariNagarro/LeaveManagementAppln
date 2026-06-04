using System.ComponentModel.DataAnnotations;

namespace LeaveService.Models;

/// <summary>
/// Request DTO for approving/rejecting leave
/// </summary>
public class ApproveRejectRequest
{
    [MaxLength(500, ErrorMessage = "Comments cannot exceed 500 characters")]
    public string? Comments { get; set; }
}
