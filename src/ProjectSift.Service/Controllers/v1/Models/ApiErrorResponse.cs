namespace ProjectSift.Service.Controllers.v1.Models;

public sealed record ApiErrorResponse(string Code, string Message, string? BatchId = null, IReadOnlyList<string>? Details = null);
