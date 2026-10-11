using System.Runtime.InteropServices;
using ProjectSift.Application.Models;

namespace ProjectSift.Service.BackgroundServices;

internal static class PayloadMemoryReleaser
{
    public static void ClearPayloadBuffers(IReadOnlyList<PayloadInput> payloads)
    {
        ArgumentNullException.ThrowIfNull(payloads);

        for (var index = 0; index < payloads.Count; index++)
        {
            var content = payloads[index].Content;
            if (!MemoryMarshal.TryGetArray(content, out var segment) || segment.Array is null)
            {
                continue;
            }

            var span = segment.Array.AsSpan(segment.Offset, segment.Count);
            span.Clear();
        }
    }
}
