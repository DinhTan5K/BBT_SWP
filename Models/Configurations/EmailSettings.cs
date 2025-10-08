namespace start.Models.Configurations
{
    public class EmailSettings
    {
        public string FromEmail { get; set; } = "";
        public string AppPassword { get; set; } = "";
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string AdminEmail { get; set; } = "";
    }
}
