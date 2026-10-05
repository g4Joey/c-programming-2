using LeaveMate.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Data
{
    /// <summary>
    /// Backend track's minimal contract with persistence. Table design,
    /// indexing, and migrations are owned by the DBA / Data Access Engineer
    /// tracks; this context exists so the validation engine, workflow
    /// service, and controller have something concrete to query against.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>()
                .Property(e => e.SickLeaveBalanceDays)
                .HasDefaultValue(5);

            modelBuilder.Entity<Employee>()
                .Property(e => e.PersonalLeaveBalanceDays)
                .HasDefaultValue(2);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Supervisor)
                .WithMany()
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveRequest>()
                .Property(r => r.Type)
                .HasConversion<string>();

            modelBuilder.Entity<LeaveRequest>()
                .Property(r => r.Status)
                .HasConversion<string>();

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.Property(log => log.Action)
                    .HasMaxLength(64)
                    .IsRequired();
                entity.Property(log => log.PreviousStatus)
                    .HasConversion<string>()
                    .HasMaxLength(64);
                entity.Property(log => log.NewStatus)
                    .HasConversion<string>()
                    .HasMaxLength(64);

                entity.HasOne(log => log.LeaveRequest)
                    .WithMany()
                    .HasForeignKey(log => log.LeaveRequestId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Employee>()
                    .WithMany()
                    .HasForeignKey(log => log.ActorEmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(log => new { log.LeaveRequestId, log.OccurredAtUtc });
                entity.HasIndex(log => log.ActorEmployeeId);
            });
        }
    }
}
