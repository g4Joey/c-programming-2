namespace LeaveMate.Services.Email
{
    public sealed record EmailDeliveryResult(bool Sent, bool Skipped, string? Error)
    {
        public static EmailDeliveryResult Success() => new(true, false, null);
        public static EmailDeliveryResult Skip() => new(false, true, null);
        public static EmailDeliveryResult Failure(string error) => new(false, false, error);
    }
}
