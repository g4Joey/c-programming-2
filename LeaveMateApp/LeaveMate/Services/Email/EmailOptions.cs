namespace LeaveMate.Services.Email
{
    public sealed class EmailOptions
    {
        public bool Enabled { get; set; }
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "LeaveMate";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
