namespace RxFlow.Infrastructure.Integrations;

public sealed class ConnectorOptions
{
    public string PricingBaseUrl { get; init; } = string.Empty;
    public string LabBaseUrl { get; init; } = string.Empty;
    public string CoatingBaseUrl { get; init; } = string.Empty;
    public string ShipmentBaseUrl { get; init; } = string.Empty;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
