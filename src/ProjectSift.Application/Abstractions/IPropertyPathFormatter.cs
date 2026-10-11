namespace ProjectSift.Application.Abstractions;

public interface IPropertyPathFormatter
{
    string AppendPropertySegment(string parentPath, string segment);
    string AppendArraySegment(string parentPath);
}
