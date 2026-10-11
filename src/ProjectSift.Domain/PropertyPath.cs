namespace ProjectSift.Domain;

/// <summary>
/// Represents a single extracted JSON property path.
/// 
/// Escaping convention:
/// - Use dot notation for simple segments (example: customer.id).
/// - Use [] to represent array traversal (example: items[].sku).
/// - For property names containing reserved characters (such as '.', '[' or ']'),
///   use bracket-quoted segments: ['segment.with.specials'].
/// - Inside bracket-quoted segments, escape backslash and single quote with '\\' and '\''.
/// </summary>
public sealed record PropertyPath
{
    public PropertyPath(string value)
    {
        Value = Guard.NotNullOrWhiteSpace(value, nameof(value));
    }
    public string Value { get; }
    public override string ToString() => Value;
}
