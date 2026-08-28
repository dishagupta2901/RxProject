namespace RxFlow.Domain;

public sealed record Prescription
{
    private Prescription() { }
    public decimal Sphere { get; private set; }
    public decimal Cylinder { get; private set; }
    public int Axis { get; private set; }

    public Prescription(decimal sphere, decimal cylinder, int axis)
    {
        if (axis is < 0 or > 180) throw new ArgumentOutOfRangeException(nameof(axis));
        Sphere = sphere; Cylinder = cylinder; Axis = axis;
    }
}
