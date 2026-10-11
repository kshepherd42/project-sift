using ProjectSift.Application;
using ProjectSift.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(ServiceConstants.Cors.ViteDevServerPolicyName, policy =>
    {
        policy.WithOrigins(ServiceConstants.Cors.ViteDevServerOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IHealthStatusProvider, HealthStatusProvider>();

var app = builder.Build();

app.UseCors(ServiceConstants.Cors.ViteDevServerPolicyName);

app.MapGet(ServiceConstants.Routes.Health, (IHealthStatusProvider healthStatusProvider) =>
{
    var status = healthStatusProvider.GetStatus();
    return Results.Ok(new Dictionary<string, string>
    {
        [ServiceConstants.ResponseFields.Status] = status.Status
    });
});

app.Run();

public partial class Program;
