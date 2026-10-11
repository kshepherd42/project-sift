using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Models;

namespace ProjectSift.Application.Providers;

public sealed class BinaryJsonBoundaryScanner(BinaryJsonExtractionOptions options) : IBinaryJsonBoundaryScanner
{
    private readonly BinaryJsonExtractionOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public BoundaryScanResult Scan(
        byte[] content,
        int offset,
        int contentLength,
        int startPosition,
        CancellationToken cancellationToken = default)
    {
        Span<byte> delimiters = stackalloc byte[_options.MaxNestingDepth];
        var depth = 0;
        var insideString = false;
        var escaped = false;

        for (var position = startPosition; position < contentLength; position++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = content[offset + position];
            if ((position - startPosition + 1) > _options.MaxCandidateBytes)
            {
                return BoundaryScanResult.TooLarge(position);
            }

            if (insideString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == (byte)'\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == (byte)'"')
                {
                    insideString = false;
                }

                continue;
            }

            if (current == (byte)'"')
            {
                insideString = true;
                continue;
            }

            if (current is (byte)'{' or (byte)'[')
            {
                if (depth == _options.MaxNestingDepth)
                {
                    return BoundaryScanResult.TooDeep(position);
                }

                delimiters[depth] = current;
                depth++;
                continue;
            }

            if (current is (byte)'}' or (byte)']')
            {
                if (depth == 0)
                {
                    return BoundaryScanResult.Mismatched(position);
                }

                var open = delimiters[depth - 1];
                if (!IsMatchingDelimiter(open, current))
                {
                    return BoundaryScanResult.Mismatched(position);
                }

                depth--;
                if (depth == 0)
                {
                    return BoundaryScanResult.Complete(position);
                }
            }
        }

        if (depth == 0)
        {
            return BoundaryScanResult.Ambiguous();
        }

        if (insideString || escaped)
        {
            return BoundaryScanResult.Ambiguous();
        }

        var recoveryClosers = new List<byte>(depth);
        for (var index = depth - 1; index >= 0; index--)
        {
            recoveryClosers.Add(delimiters[index] == (byte)'{' ? (byte)'}' : (byte)']');
        }

        return BoundaryScanResult.Recoverable(recoveryClosers);
    }

    private static bool IsMatchingDelimiter(byte open, byte close)
        => (open == (byte)'{' && close == (byte)'}')
            || (open == (byte)'[' && close == (byte)']');
}
