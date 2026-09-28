using LeaveMate.Models;
using Microsoft.AspNetCore.Http;

namespace LeaveMate.Services;

public static class DemoSessionExtensions
{
    public const string EmployeeIdKey = "DemoEmployeeId";
    public const string EmployeeNameKey = "DemoEmployeeName";
    public const string EmployeeRoleKey = "DemoEmployeeRole";

    public static void SetActiveEmployee(this ISession session, Employee employee, string role)
    {
        session.SetInt32(EmployeeIdKey, employee.Id);
        session.SetString(EmployeeNameKey, employee.FullName);
        session.SetString(EmployeeRoleKey, role);
    }

    public static int? GetActiveEmployeeId(this ISession session)
    {
        return session.GetInt32(EmployeeIdKey);
    }

    public static string? GetActiveEmployeeName(this ISession session)
    {
        return session.GetString(EmployeeNameKey);
    }

    public static string? GetActiveEmployeeRole(this ISession session)
    {
        return session.GetString(EmployeeRoleKey);
    }

    public static bool IsActiveRole(this ISession session, string role)
    {
        return string.Equals(session.GetActiveEmployeeRole(), role, StringComparison.OrdinalIgnoreCase);
    }
}
