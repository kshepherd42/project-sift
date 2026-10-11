using Riok.Mapperly.Abstractions;
using ProjectSift.Domain.Models;

namespace ProjectSift.Infrastructure.Models;

[Mapper]
internal static partial class InfrastructureBatchResultMapper
{
    public static partial InfrastructureBatchResult ToInfrastructure(BatchResult source);

    public static partial BatchResult ToDomain(InfrastructureBatchResult source);
}
