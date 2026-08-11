using System;
using LeaveMate.Enums;
using LeaveMate.Models;

namespace LeaveMate.DTOs
{
    public class LeaveRequestResponseDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public LeaveType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationInDays { get; set; }
        public LeaveStatus Status { get; set; }
        public string? Reason { get; set; }
        public string? SupervisorComment { get; set; }
        public string? HrComment { get; set; }
        public DateTime SubmittedAtUtc { get; set; }

        public static LeaveRequestResponseDto FromEntity(LeaveRequest r) => new()
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeName = r.Employee?.FullName ?? string.Empty,
            Type = r.Type,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            DurationInDays = r.DurationInDays,
            Status = r.Status,
            Reason = r.Reason,
            SupervisorComment = r.SupervisorComment,
            HrComment = r.HrComment,
            SubmittedAtUtc = r.SubmittedAtUtc
        };
    }
}
