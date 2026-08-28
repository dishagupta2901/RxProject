namespace RxFlow.Domain;

public enum OrderStatus { Submitted, Validated, Routed, Scheduled, Shipped, Rejected }

public sealed record Frame
{
    private Frame() { }
    public string Id { get; private set; } = string.Empty;
    public decimal A { get; private set; }
    public decimal B { get; private set; }
    public Frame(string id, decimal a, decimal b)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Frame id is required.", nameof(id));
        if (a <= 0 || b <= 0) throw new ArgumentOutOfRangeException(nameof(a));
        Id = id; A = a; B = b;
    }
}

public sealed class LensOrder
{
    private LensOrder() { }
    public Guid Id { get; private set; }
    public Prescription Prescription { get; private set; } = null!;
    public Frame Frame { get; private set; } = null!;
    public OrderStatus Status { get; private set; } = OrderStatus.Submitted;

    public LensOrder(Guid id, Prescription prescription, Frame frame)
    {
        if (id == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(id));
        Id = id;
        Prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
        Frame = frame ?? throw new ArgumentNullException(nameof(frame));
    }

    public void ValidateGrindability(decimal maxAbsolutePower)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAbsolutePower);
        var power = Math.Max(Math.Abs(Prescription.Sphere), Math.Abs(Prescription.Sphere + Prescription.Cylinder));
        Status = power <= maxAbsolutePower ? OrderStatus.Validated : OrderStatus.Rejected;
    }

    public void TransitionTo(OrderStatus next)
    {
        if (Status == OrderStatus.Rejected) throw new InvalidOperationException("Rejected orders cannot progress.");
        if ((int)next < (int)Status) throw new InvalidOperationException("Order status cannot move backwards.");
        Status = next;
    }
}
