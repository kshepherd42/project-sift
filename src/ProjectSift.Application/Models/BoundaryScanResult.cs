using ProjectSift.Application.Enums;

namespace ProjectSift.Application.Models;

public sealed record BoundaryScanResult(BoundaryScanStatus Status, int EndPosition, int? ErrorPosition, IReadOnlyList<byte> RecoveryClosers)
{
    public static BoundaryScanResult Complete(int endPosition)
        => new(BoundaryScanStatus.Complete, endPosition, null, []);

    public static BoundaryScanResult Recoverable(IReadOnlyList<byte> recoveryClosers)
        => new(BoundaryScanStatus.RecoverableIncomplete, -1, null, recoveryClosers);

    public static BoundaryScanResult Ambiguous()
        => new(BoundaryScanStatus.AmbiguousIncomplete, -1, null, []);

    public static BoundaryScanResult Mismatched(int position)
        => new(BoundaryScanStatus.MismatchedBoundary, -1, position, []);

    public static BoundaryScanResult TooDeep(int position)
        => new(BoundaryScanStatus.TooDeep, -1, position, []);

    public static BoundaryScanResult TooLarge(int position)
        => new(BoundaryScanStatus.TooLarge, -1, position, []);
}
