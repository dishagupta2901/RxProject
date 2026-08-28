using RxFlow.Domain;

namespace RxFlow.Reporting;

/// <summary>
/// A read-only projection of an order for reporting/status purposes. This is intentionally not the
/// write-model <see cref="LensOrder"/> entity: it exposes only the fields a status view needs and
/// carries no persistence identity or mutation surface, so reading through it can never become a
/// back door into application write models (see architecture.md, RxFlow.Reporting responsibilities).
/// </summary>
public sealed record OrderStatusView(
    Guid OrderId,
    OrderStatus Status,
    string FrameId,
    decimal Sphere,
    decimal Cylinder,
    int Axis);
