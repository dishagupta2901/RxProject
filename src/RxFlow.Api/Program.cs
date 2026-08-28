using RxFlow.Application;
using RxFlow.Api;
using Microsoft.EntityFrameworkCore;
using RxFlow.Infrastructure.Persistence;
using Hangfire;
using Hangfire.MemoryStorage;
using RxFlow.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using RxFlow.Infrastructure.Integrations;
using RxFlow.Infrastructure.Coordination;
using StackExchange.Redis;
using Confluent.Kafka;
using RxFlow.Infrastructure.Messaging;
using RxFlow.Infrastructure.Reporting;
using RxFlow.Reporting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Auth:Authority"];
    options.Audience = builder.Configuration["Auth:Audience"] ?? "rxflow-api";
    options.RequireHttpsMetadata = false;
});
builder.Services.AddAuthorization(options =>
    options.AddPolicy("LabOverride", policy => policy.RequireRole("lab-override")));
builder.Services.AddSingleton<IPriceCalculator, LocalPriceCalculator>();
builder.Services.AddSingleton<IOrderWorkDispatcher, LocalWorkDispatcher>();
builder.Services.AddScoped<SubmitOrderService>();
builder.Services.AddScoped<CancelOrderService>();
builder.Services.AddHangfire(configuration => configuration.UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddScoped<OrderWorkflowJob>();
builder.Services.AddScoped<IOrderWorkDispatcher, HangfireOrderWorkDispatcher>();
builder.Services.Configure<ConnectorOptions>(builder.Configuration.GetSection("Connectors"));
builder.Services.AddHttpClient<IPricingClient, PricingClient>((sp, client) =>
    client.BaseAddress = new Uri(builder.Configuration["Connectors:PricingBaseUrl"] ?? "http://pricing-fake"));
builder.Services.AddHttpClient<ILabCapabilityClient, LabCapabilityClient>((sp, client) =>
    client.BaseAddress = new Uri(builder.Configuration["Connectors:LabBaseUrl"] ?? "http://lab-fake"));
builder.Services.AddHttpClient<ICoatingClient, CoatingClient>((sp, client) =>
    client.BaseAddress = new Uri(builder.Configuration["Connectors:CoatingBaseUrl"] ?? "http://coating-fake"));
builder.Services.AddHttpClient<IShipmentClient, ShipmentClient>((sp, client) =>
    client.BaseAddress = new Uri(builder.Configuration["Connectors:ShipmentBaseUrl"] ?? "http://shipment-fake"));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:Configuration"] ?? "localhost:6379,abortConnect=false"));
builder.Services.AddSingleton<IProducer<string, string>>(_ => new ProducerBuilder<string, string>(
    new ProducerConfig { BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092" }).Build());
builder.Services.AddSingleton<KafkaEventPublisher>(sp => new KafkaEventPublisher(
    sp.GetRequiredService<IProducer<string, string>>(), builder.Configuration["Kafka:Topic"] ?? "rxflow.order.v1"));
builder.Services.AddScoped<OutboxDispatcher>();
if (builder.Configuration.GetValue<bool>("Persistence:ApplyMigrations"))
{
    builder.Services.AddScoped<IOutboxWriter, EfOutboxWriter>();
    builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
    builder.Services.AddScoped<IOrderReportReader, EfOrderReportReader>();
}
else
{
    builder.Services.AddScoped<IOutboxWriter, LocalOutboxWriter>();
    builder.Services.AddSingleton<LocalOrderRepository>();
    builder.Services.AddSingleton<IOrderRepository>(sp => sp.GetRequiredService<LocalOrderRepository>());
    builder.Services.AddSingleton<IOrderReportReader>(sp => sp.GetRequiredService<LocalOrderRepository>());
}
builder.Services.AddScoped<OrderReportingService>();
builder.Services.AddScoped<OutboxDispatchJob>();
builder.Services.AddDbContext<RxFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RxFlow")));
var app = builder.Build();
if (app.Configuration.GetValue<bool>("Persistence:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<RxFlowDbContext>().Database.Migrate();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("frontend");
app.MapControllers();
app.UseHangfireDashboard("/hangfire");
RecurringJob.AddOrUpdate<OutboxDispatchJob>("rxflow-outbox", job => job.ExecuteAsync(CancellationToken.None), Cron.Minutely);
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();

public partial class Program { }
