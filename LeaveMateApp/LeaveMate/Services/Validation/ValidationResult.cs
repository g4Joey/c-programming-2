using System.Collections.Generic;
using System.Linq;

namespace LeaveMate.Services.Validation
{
    /// <summary>
    /// Aggregates every rule violation found by the validation engine so the
    /// controller can return a single, complete error response instead of
    /// failing fast on the first broken rule.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; } = new();

        public void AddError(string message) => Errors.Add(message);

        public static ValidationResult Success() => new();
    }
}
