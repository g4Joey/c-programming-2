using LeaveMate.Enums;
using LeaveMate.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LeaveMate.Data
{
    /// <summary>
    /// Seeds a minimal but realistic dataset so the app is usable on first
    /// run: an HR Administrator, two supervisors, and employees reporting to
    /// them, matching the multi-tier approval model. Owned by the Database
    /// Administrator (table/key design) and Data Access Engineer (EF Core
    /// model wiring) tracks.
    /// </summary>
    public static class DbSeeder
    {
        // Demo-only password shared by every seeded account so the team can
        // sign in while testing. Replace before any real deployment.
        public const string DemoPassword = "Password123!";

        public static void Seed(ApplicationDbContext db)
        {
            db.Database.EnsureCreated();
            EnsureAuditLogTable(db);
            EnsureAdditionalLeaveBalanceColumns(db);

            var hrAdmin = db.Employees.FirstOrDefault(e => e.Email == "ama.boateng@leavemate.local") ?? new Employee
            {
                FullName = "Ama Boateng",
                Email = "ama.boateng@leavemate.local",
                IsHrAdministrator = true,
                AnnualLeaveBalanceDays = 21
            };

            var supervisor = db.Employees.FirstOrDefault(e => e.Email == "kojo.mensah@leavemate.local") ?? new Employee
            {
                FullName = "Kojo Mensah",
                Email = "kojo.mensah@leavemate.local",
                AnnualLeaveBalanceDays = 21
            };

            hrAdmin.IsHrAdministrator = true;
            hrAdmin.SupervisorId = null;
            supervisor.IsHrAdministrator = false;
            supervisor.SupervisorId = null;

            if (hrAdmin.Id == 0) db.Employees.Add(hrAdmin);
            if (supervisor.Id == 0) db.Employees.Add(supervisor);
            db.SaveChanges();

            var jane = db.Employees.FirstOrDefault(e => e.Email == "jane.mensah@leavemate.local") ?? new Employee
            {
                FullName = "Jane Mensah",
                Email = "jane.mensah@leavemate.local",
                AnnualLeaveBalanceDays = 21
            };

            jane.SupervisorId = supervisor.Id;
            jane.IsHrAdministrator = false;
            if (jane.Id == 0) db.Employees.Add(jane);
            db.SaveChanges();

            var manager = db.Employees.First(e => e.Email == "kojo.mensah@leavemate.local");
            var janeEmployee = db.Employees.First(e => e.Email == "jane.mensah@leavemate.local");
            var kendall = db.Employees.FirstOrDefault(e => e.Email == "kendall.brooks@leavemate.local") ?? new Employee
            {
                FullName = "Kendall Brooks",
                Email = "kendall.brooks@leavemate.local",
                SupervisorId = manager.Id,
                AnnualLeaveBalanceDays = 18
            };

            var james = db.Employees.FirstOrDefault(e => e.Email == "james.agyemang@leavemate.local") ?? new Employee
            {
                FullName = "James Osei Agyemang",
                Email = "james.agyemang@leavemate.local",
                SupervisorId = manager.Id,
                AnnualLeaveBalanceDays = 21
            };

            if (kendall.Id == 0) db.Employees.Add(kendall);
            if (james.Id == 0) db.Employees.Add(james);
            db.SaveChanges();

            if (!db.LeaveRequests.Any(r => r.EmployeeId == janeEmployee.Id))
            {
                db.LeaveRequests.Add(new LeaveRequest
                {
                    EmployeeId = janeEmployee.Id,
                    Type = LeaveType.Annual,
                    StartDate = DateTime.UtcNow.Date.AddDays(14),
                    EndDate = DateTime.UtcNow.Date.AddDays(18),
                    Reason = "Family event",
                    Status = LeaveStatus.PendingSupervisorApproval,
                    SubmittedAtUtc = DateTime.UtcNow
                });
            }

            db.SaveChanges();

            // Give every account without a password the demo password (hashed).
            var hasher = new PasswordHasher<Employee>();
            foreach (var employee in db.Employees.Where(e => e.PasswordHash == ""))
            {
                employee.PasswordHash = hasher.HashPassword(employee, DemoPassword);
            }

            db.SaveChanges();
        }

        private static void EnsureAuditLogTable(ApplicationDbContext db)
        {
            if (db.Database.IsSqlite())
            {
                db.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS "AuditLogs" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_AuditLogs" PRIMARY KEY AUTOINCREMENT,
                        "LeaveRequestId" INTEGER NOT NULL,
                        "ActorEmployeeId" INTEGER NOT NULL,
                        "Action" TEXT NOT NULL,
                        "PreviousStatus" TEXT NOT NULL,
                        "NewStatus" TEXT NOT NULL,
                        "OccurredAtUtc" TEXT NOT NULL,
                        CONSTRAINT "FK_AuditLogs_LeaveRequests_LeaveRequestId"
                            FOREIGN KEY ("LeaveRequestId") REFERENCES "LeaveRequests" ("Id") ON DELETE RESTRICT,
                        CONSTRAINT "FK_AuditLogs_Employees_ActorEmployeeId"
                            FOREIGN KEY ("ActorEmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
                    );
                    CREATE INDEX IF NOT EXISTS "IX_AuditLogs_LeaveRequestId_OccurredAtUtc"
                        ON "AuditLogs" ("LeaveRequestId", "OccurredAtUtc");
                    CREATE INDEX IF NOT EXISTS "IX_AuditLogs_ActorEmployeeId"
                        ON "AuditLogs" ("ActorEmployeeId");
                    """);
                return;
            }

            if (db.Database.IsSqlServer())
            {
                db.Database.ExecuteSqlRaw("""
                    IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[AuditLogs] (
                            [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AuditLogs] PRIMARY KEY,
                            [LeaveRequestId] int NOT NULL,
                            [ActorEmployeeId] int NOT NULL,
                            [Action] nvarchar(64) NOT NULL,
                            [PreviousStatus] nvarchar(64) NOT NULL,
                            [NewStatus] nvarchar(64) NOT NULL,
                            [OccurredAtUtc] datetime2 NOT NULL,
                            CONSTRAINT [FK_AuditLogs_LeaveRequests_LeaveRequestId]
                                FOREIGN KEY ([LeaveRequestId]) REFERENCES [dbo].[LeaveRequests] ([Id]) ON DELETE NO ACTION,
                            CONSTRAINT [FK_AuditLogs_Employees_ActorEmployeeId]
                                FOREIGN KEY ([ActorEmployeeId]) REFERENCES [dbo].[Employees] ([Id]) ON DELETE NO ACTION
                        );
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_LeaveRequestId_OccurredAtUtc' AND object_id = OBJECT_ID(N'[dbo].[AuditLogs]'))
                        CREATE INDEX [IX_AuditLogs_LeaveRequestId_OccurredAtUtc] ON [dbo].[AuditLogs] ([LeaveRequestId], [OccurredAtUtc]);
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_ActorEmployeeId' AND object_id = OBJECT_ID(N'[dbo].[AuditLogs]'))
                        CREATE INDEX [IX_AuditLogs_ActorEmployeeId] ON [dbo].[AuditLogs] ([ActorEmployeeId]);
                    """);
                return;
            }

            throw new NotSupportedException(
                $"Audit log schema creation is not supported for provider '{db.Database.ProviderName}'.");
        }

        private static void EnsureAdditionalLeaveBalanceColumns(ApplicationDbContext db)
        {
            if (db.Database.IsSqlite())
            {
                var connection = db.Database.GetDbConnection();
                var closeConnection = connection.State != System.Data.ConnectionState.Open;
                if (closeConnection)
                {
                    connection.Open();
                }

                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = "PRAGMA table_info('Employees');";
                    using var reader = command.ExecuteReader();
                    var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    while (reader.Read())
                    {
                        columns.Add(reader.GetString(1));
                    }

                    reader.Close();
                    if (!columns.Contains(nameof(Employee.SickLeaveBalanceDays)))
                    {
                        db.Database.ExecuteSqlRaw(
                            "ALTER TABLE Employees ADD COLUMN SickLeaveBalanceDays INTEGER NOT NULL DEFAULT 5;");
                    }

                    if (!columns.Contains(nameof(Employee.PersonalLeaveBalanceDays)))
                    {
                        db.Database.ExecuteSqlRaw(
                            "ALTER TABLE Employees ADD COLUMN PersonalLeaveBalanceDays INTEGER NOT NULL DEFAULT 2;");
                    }
                }
                finally
                {
                    if (closeConnection)
                    {
                        connection.Close();
                    }
                }

                return;
            }

            if (db.Database.IsSqlServer())
            {
                db.Database.ExecuteSqlRaw("""
                    IF COL_LENGTH('Employees', 'SickLeaveBalanceDays') IS NULL
                        ALTER TABLE Employees ADD SickLeaveBalanceDays INT NOT NULL
                            CONSTRAINT DF_Employees_SickLeaveBalanceDays DEFAULT 5;
                    IF COL_LENGTH('Employees', 'PersonalLeaveBalanceDays') IS NULL
                        ALTER TABLE Employees ADD PersonalLeaveBalanceDays INT NOT NULL
                            CONSTRAINT DF_Employees_PersonalLeaveBalanceDays DEFAULT 2;
                    """);
                return;
            }

            throw new NotSupportedException(
                $"Leave balance schema updates are not supported for provider '{db.Database.ProviderName}'.");
        }
    }
}