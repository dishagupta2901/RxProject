using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RxFlow.Infrastructure.Persistence;

public sealed class RxFlowDbContextFactory : IDesignTimeDbContextFactory<RxFlowDbContext>
{
    public RxFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RxFlowDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=rxflow;Username=rxflow;Password=rxflow")
            .Options;
        return new RxFlowDbContext(options);
    }
}
