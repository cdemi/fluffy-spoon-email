namespace fluffyspoon.email.ViewModels
{
    public class SmtpOptions
    {
        public const string DefaultSectionName = "Smtp";
        
        public string Hostname { get; set; }

        public string Username { get; set; }
        
        public string Password { get; set; }
    }
}