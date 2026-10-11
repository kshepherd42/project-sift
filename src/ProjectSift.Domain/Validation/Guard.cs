namespace ProjectSift.Domain.Validation;

public static class Guard
{
    private const string NullOrWhiteSpaceMessage = "Value cannot be null, empty, or whitespace.";
    private const string NegativeValueMessage = "Value cannot be negative.";
    private const string NonPositiveValueMessage = "Value must be positive.";

    public static string NotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(NullOrWhiteSpaceMessage, paramName);
        }

        return value;
    }

    public static int NonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, NegativeValueMessage);
        }

        return value;
    }

    public static int Positive(int value, string paramName)
    {
        NonNegative(value, paramName);
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(paramName, NonPositiveValueMessage);
        }

        return value;
    }

    public static int? NonNegative(int? value, string paramName)
    {
        if (value is < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, NegativeValueMessage);
        }

        return value;
    }
}
