using System;
using System.ComponentModel.DataAnnotations;
using LeaveMate.Enums;

namespace LeaveMate.DTOs
{
    public class CreateLeaveRequestDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public LeaveType Type { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
