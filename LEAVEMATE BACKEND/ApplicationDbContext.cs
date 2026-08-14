using LeaveMate.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Data
{
    /// <summary>
    /// Database context for LeaveMate.
    /// Handles employees, leave requests and audit logs.
    /// </summary>
    public class ApplicationDbContext : 
    DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees => Set<Employee>();

        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            // Employee -> Supervisor relationship
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Supervisor)
                .WithMany()
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee -> Leave Requests
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Leave Type stored as text
            modelBuilder.Entity<LeaveRequest>()
                .Property(r => r.Type)
                .HasConversion<string>();

            // Leave Status stored as text
            modelBuilder.Entity<LeaveRequest>()
                .Property(r => r.Status)
                .HasConversion<string>();

            // Audit Log configuration
            modelBuilder.Entity<AuditLog>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Action)
                .IsRequired()
                .HasMaxLength(200);

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Details)
                .HasMaxLength(1000);

            // AuditLog -> LeaveRequest
            modelBuilder.Entity<AuditLog>()
                .HasOne<LeaveRequest>()
                .WithMany()
                .HasForeignKey(a => a.LeaveRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}