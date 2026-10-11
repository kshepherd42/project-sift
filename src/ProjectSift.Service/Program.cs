using ProjectSift.Application;

var builder = WebApplication.CreateBuilder(args);

const string viteCorsPolicy = "ViteDevServer";

builder.Services.AddCors(options =>
{
    options.AddPolicy(viteCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IHealthStatusProvider, HealthStatusProvider>();

var app = builder.Build();

app.UseCors(viteCorsPolicy);

app.MapGet("/api/health", (IHealthStatusProvider healthStatusProvider) =>
{
    var status = healthStatusProvider.GetStatus();
    return Results.Ok(new { status = status.Status });
});

app.Run();

public partial class Program;
