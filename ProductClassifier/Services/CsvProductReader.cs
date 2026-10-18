using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ProductClassifier.Models;

namespace ProductClassifier.Services;

public class CsvProductReader
{
    private static readonly CsvConfiguration Config = new(CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
        // Nagłówki w pliku są małymi literami (sku, nazwa...), a parametry rekordu w PascalCase.
        PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
        TrimOptions = TrimOptions.Trim,
    };

    public IReadOnlyList<Product> Read(string path)
    {
        using var reader = new StreamReader(path, Encoding.UTF8);
        using var csv = new CsvReader(reader, Config);

        return csv.GetRecords<Product>()
            .Select(Normalize)
            .ToList();
    }

    // CsvHelper zwraca puste pola jako "", zamieniamy je na null, żeby brak danych był jawny.
    private static Product Normalize(Product p) => p with
    {
        Opis = NullIfEmpty(p.Opis),
        Producent = NullIfEmpty(p.Producent),
    };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
