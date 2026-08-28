using Microsoft.EntityFrameworkCore;
using RxFlow.Domain;

namespace RxFlow.Infrastructure.Persistence;

public sealed class RxFlowDbContext(DbContextOptions<RxFlowDbContext> options) : DbContext(options)
{
    public DbSet<LensOrder> Orders => Set<LensOrder>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<LensOrder>();
        order.HasKey(x => x.Id);
        order.Property(x => x.Status).HasConversion<string>().IsRequired();
        order.OwnsOne(x => x.Prescription);
        order.OwnsOne(x => x.Frame);
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.DispatchedAt);
            entity.Property(x => x.EventType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AggregateId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
        });
    }
}
