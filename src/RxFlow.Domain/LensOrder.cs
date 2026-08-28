namespace RxFlow.Domain;

public enum OrderStatus { Submitted, Validated, Routed, Scheduled, Shipped, Rejected }

public sealed record Frame
{
    public string Id { get; }
    public decimal A { get; }
    public decimal B { get; }
    public Frame(string id, decimal a, decimal b)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Frame id is required.", nameof(id));
        if (a <= 0 || b <= 0) throw new ArgumentOutOfRangeException(nameof(a));
        Id = id; A = a; B = b;
    }
}

public sealed class LensOrder
{
    public Guid Id { get; }
    public Prescription Prescription { get; }
    public Frame Frame { get; }
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
        if (maxAbsolutePower <= 0) throw new ArgumentOutOfRangeException(nameof(maxAbsolutePower));
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
