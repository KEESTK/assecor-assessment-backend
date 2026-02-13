using System.Text;
using Assecor.Assessment.Application.Ports;

namespace Assecor.Assessment.Infrastructure.Csv;

public sealed class CsvPersonSource : IPersonCsvSource
{
    private readonly string _filePath;

    public CsvPersonSource(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("CSV file path must not be empty.", nameof(filePath));

        _filePath = filePath;
    }

    public async Task<IReadOnlyList<PersonCsvRecord>> ReadAllAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"CSV file not found at '{_filePath}'.", _filePath);

        var result = new List<PersonCsvRecord>();
        var buffer = string.Empty;

        using var stream = File.OpenRead(_filePath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync();
            if (line is null) break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var trimmedLine = line.Trim();

            buffer = string.IsNullOrEmpty(buffer)
                ? trimmedLine
                : $"{buffer} {trimmedLine}"; // join split records safely

            if (CountCommas(buffer) < 3)
                continue;

            var parts = buffer.Split(',', StringSplitOptions.TrimEntries);

            if (parts.Length != 4)
                throw new FormatException($"Invalid CSV record (expected 4 fields): '{buffer}'");

            if (!int.TryParse(parts[3], out var colourCode))
                throw new FormatException($"Invalid colour code (expected int): '{buffer}'");

            result.Add(new PersonCsvRecord(
                LastName: parts[0],
                FirstName: parts[1],
                ZipAndCity: parts[2],
                ColourCode: colourCode
            ));

            buffer = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(buffer))
            throw new FormatException($"Incomplete CSV record at end of file: '{buffer}'");

        return result;
    }

    private static int CountCommas(string s)
    {
        var count = 0;
        foreach (var ch in s)
            if (ch == ',') count++;
        return count;
    }
}