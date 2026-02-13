namespace Assecor.Assessment.Domain.Colours;

public readonly record struct FavouriteColour(string Value)
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "blau",
        "grün",
        "violett",
        "rot",
        "gelb",
        "türkis",
        "weiß"
    };

    public static FavouriteColour From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FavouriteColour must not be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        // Accept common ASCII variants and map to canonical German names
        normalized = normalized switch
        {
            "gruen" => "grün",
            "tuerkis" => "türkis",
            "weiss" => "weiß",
            _ => normalized
        };

        if (!Allowed.Contains(normalized))
            throw new ArgumentException(
                $"Invalid colour '{value}'. Allowed values: {string.Join(", ", Allowed)}",
                nameof(value));

        return new FavouriteColour(normalized);
    }

    public override string ToString() => Value;
}