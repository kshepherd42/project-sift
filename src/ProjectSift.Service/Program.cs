using ProjectSift.Application;
using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Models;
using ProjectSift.Application.Providers;
using ProjectSift.Infrastructure;
using ProjectSift.Service.BackgroundServices;
using ProjectSift.Service.Configuration;
using ProjectSift.Service.Constants;
using ProjectSift.Service.Filters;
using ProjectSift.Service.Validation;

var builder = WebApplication.CreateBuilder(args);

var uploadLimits = builder.Configuration
    .GetSection(ServiceConstants.Configuration.UploadLimitsSectionName)
    .Get<UploadLimitsOptions>() ?? new UploadLimitsOptions();

builder.Services.Configure<UploadLimitsOptions>(builder.Configuration.GetSection(ServiceConstants.Configuration.UploadLimitsSectionName));
builder.Services.Configure<BackgroundProcessingOptions>(builder.Configuration.GetSection(ServiceConstants.Configuration.BackgroundProcessingSectionName));
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = uploadLimits.MaxRequestBytes;
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(_ =>
{
    var section = builder.Configuration.GetSection(ServiceConstants.Configuration.ExtractionSectionName);

    var maxCandidateBytes = section.GetValue<int?>(nameof(BinaryJsonExtractionOptions.MaxCandidateBytes))
        ?? BinaryJsonExtractionOptions.DefaultMaxCandidateBytes;
    var maxNestingDepth = section.GetValue<int?>(nameof(BinaryJsonExtractionOptions.MaxNestingDepth))
        ?? BinaryJsonExtractionOptions.DefaultMaxNestingDepth;
    var maxPathsPerDocument = section.GetValue<int?>(nameof(BinaryJsonExtractionOptions.MaxPathsPerDocument))
        ?? BinaryJsonExtractionOptions.DefaultMaxPathsPerDocument;
    var maxDocumentsPerPayload = section.GetValue<int?>(nameof(BinaryJsonExtractionOptions.MaxDocumentsPerPayload))
        ?? BinaryJsonExtractionOptions.DefaultMaxDocumentsPerPayload;

    return new BinaryJsonExtractionOptions(
        maxCandidateBytes,
        maxNestingDepth,
        maxPathsPerDocument,
        maxDocumentsPerPayload);
});
builder.Services.AddTransient<BinaryJsonPayloadScanner>();

builder.Services.AddScoped<BatchUploadValidator>();
builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(ServiceConstants.Cors.ViteDevServerPolicyName, policy =>
    {
        policy.WithOrigins(ServiceConstants.Cors.ViteDevServerOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddProjectSiftApplication();
builder.Services.AddProjectSiftInfrastructure();
builder.Services.AddProjectSiftBackgroundProcessing();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(ServiceConstants.Cors.ViteDevServerPolicyName);

app.MapControllers();

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
