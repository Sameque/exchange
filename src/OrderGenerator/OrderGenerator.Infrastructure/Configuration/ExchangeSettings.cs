namespace OrderGenerator.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for the FIX 4.4 exchange connection.
/// Bound from appsettings.json section "FixSettings".
/// </summary>
public sealed class ExchangeSettings
{
    public const string SectionName = "ExchangeSettings";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 9876;
    public string SenderCompId { get; set; } = "CLIENT";
    public string TargetCompId { get; set; } = "EXCHANGE";
    public int ResponseTimeoutSeconds { get; set; } = 10;
    public string ApiBaseUrl { get; set; } = "http://localhost:5000/api";
}
