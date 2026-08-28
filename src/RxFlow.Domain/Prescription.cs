namespace RxFlow.Domain;

public sealed record Prescription
{
    public decimal Sphere { get; }
    public decimal Cylinder { get; }
    public int Axis { get; }

    public Prescription(decimal sphere, decimal cylinder, int axis)
    {
        if (axis is < 0 or > 180) throw new ArgumentOutOfRangeException(nameof(axis));
        Sphere = sphere; Cylinder = cylinder; Axis = axis;
    }
}
