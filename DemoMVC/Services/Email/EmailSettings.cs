namespace DemoMVC.Services.Email;

public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "DemoMVC";
    public bool EnableSsl { get; set; } = true;
}
