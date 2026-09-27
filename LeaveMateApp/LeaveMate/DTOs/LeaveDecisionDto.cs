using System.ComponentModel.DataAnnotations;

namespace LeaveMate.DTOs
{
    public class LeaveDecisionDto
    {
        [Required]
        public int DecidedByEmployeeId { get; set; }

        [Required]
        public bool Approve { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }
    }
}
