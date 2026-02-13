namespace Assecor.Assessment.Domain.Colours;

public static class ColourCodeMapper
{
    public static FavouriteColour FromCode(int code) => code switch
    {
        1 => FavouriteColour.From("blau"),
        2 => FavouriteColour.From("grün"),
        3 => FavouriteColour.From("violett"),
        4 => FavouriteColour.From("rot"),
        5 => FavouriteColour.From("gelb"),
        6 => FavouriteColour.From("türkis"),
        7 => FavouriteColour.From("weiß"),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unsupported colour code.")
    };
}