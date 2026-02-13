namespace Assecor.Assessment.Domain.Persons;

public readonly record struct PersonId(int Value)
{
    public static PersonId From(int value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "PersonId must be greater than 0.");

        return new PersonId(value);
    }

    public override string ToString() => Value.ToString();
}