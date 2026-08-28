using RxFlow.Domain;
using Xunit;

namespace RxFlow.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void PrescriptionRejectsAxisOutsideRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Prescription(1, 0, 181));
    }

    [Fact]
    public void GrindabilityRejectsExcessivePower()
    {
        var order = new LensOrder(Guid.NewGuid(), new Prescription(20, 0, 90), new Frame("F-001", 50, 40));
        order.ValidateGrindability(12);
        Assert.Equal(OrderStatus.Rejected, order.Status);
    }

    [Fact]
    public void StatusCannotMoveBackwards()
    {
        var order = new LensOrder(Guid.NewGuid(), new Prescription(1, 0, 90), new Frame("F-001", 50, 40));
        order.TransitionTo(OrderStatus.Validated);
        Assert.Throws<InvalidOperationException>(() => order.TransitionTo(OrderStatus.Submitted));
    }
}
