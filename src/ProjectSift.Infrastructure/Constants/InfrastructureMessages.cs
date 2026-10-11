namespace ProjectSift.Infrastructure.Constants;

internal static class InfrastructureMessages
{
    public const string BatchAlreadyExists = "A batch with the same identifier already exists.";
    public const string BatchNotFound = "The batch record was not found.";
    public const string EtagMismatch = "The batch record has been modified by another operation.";
}
