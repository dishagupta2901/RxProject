using System.Net.Http.Json;
using RxFlow.Domain;

namespace RxFlow.Infrastructure.Integrations;

public sealed record LabCapability(string LabId, decimal MaxPower, int ActiveJobs);
public sealed record CoatingBooking(string BookingId, DateTimeOffset ScheduledAt);
public sealed record ShipmentBooking(string TrackingId);
public interface IPricingClient
{
    Task<decimal> CalculateAsync(Guid orderId, CancellationToken cancellationToken);
}

public sealed class PricingClient(HttpClient client) : IPricingClient
{
    public async Task<decimal> CalculateAsync(Guid orderId, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync("/prices", new { orderId }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<decimal>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

public interface ILabCapabilityClient
{
    Task<IReadOnlyList<LabCapability>> GetCapabilitiesAsync(CancellationToken cancellationToken);
}

public interface ICoatingClient
{
    Task<CoatingBooking> ScheduleAsync(Guid orderId, CancellationToken cancellationToken);
}

public interface IShipmentClient
{
    Task<ShipmentBooking> CreateAsync(Guid orderId, CancellationToken cancellationToken);
}

public sealed class LabCapabilityClient(HttpClient client) : ILabCapabilityClient
{
    public async Task<IReadOnlyList<LabCapability>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        => await client.GetFromJsonAsync<IReadOnlyList<LabCapability>>("/capabilities", cancellationToken).ConfigureAwait(false)
           ?? [];
}

public sealed class CoatingClient(HttpClient client) : ICoatingClient
{
    public async Task<CoatingBooking> ScheduleAsync(Guid orderId, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync("/coatings", new { orderId }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CoatingBooking>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Coating service returned no booking.");
    }
}

public sealed class ShipmentClient(HttpClient client) : IShipmentClient
{
    public async Task<ShipmentBooking> CreateAsync(Guid orderId, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync("/shipments", new { orderId }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShipmentBooking>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Shipment service returned no booking.");
    }
}
