using System;

namespace LeaveMate.Exceptions
{
    /// <summary>Thrown when a requested state transition breaks workflow rules.</summary>
    public class WorkflowException : Exception
    {
        public WorkflowException(string message) : base(message) { }
    }
}
