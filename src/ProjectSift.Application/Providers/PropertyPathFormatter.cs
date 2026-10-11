using System.Text;
using ProjectSift.Application.Abstractions;

namespace ProjectSift.Application.Providers;

public sealed class PropertyPathFormatter : IPropertyPathFormatter
{
    public string AppendPropertySegment(string parentPath, string segment)
    {
        var escaped = IsSimplePropertyName(segment)
            ? segment
            : ToBracketQuotedSegment(segment);

        if (string.IsNullOrEmpty(parentPath))
        {
            return escaped;
        }

        if (escaped.StartsWith("[", StringComparison.Ordinal))
        {
            return string.Concat(parentPath, escaped);
        }

        return string.Concat(parentPath, ".", escaped);
    }

    public string AppendArraySegment(string parentPath)
        => string.IsNullOrEmpty(parentPath)
            ? "[]"
            : string.Concat(parentPath, "[]");

    private static bool IsSimplePropertyName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (!(char.IsLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }

        foreach (var character in name)
        {
            if (!(char.IsLetterOrDigit(character) || character == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static string ToBracketQuotedSegment(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        builder.Append("['");

        foreach (var character in name)
        {
            if (character is '\\' or '\'')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        builder.Append("']");
        return builder.ToString();
    }
}
