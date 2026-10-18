using System.Text;
using CsvHelper;
using ProductClassifier.Models;
using ProductClassifier.Services;

Console.OutputEncoding = Encoding.UTF8;

var path = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "Data", "products.csv");

IReadOnlyList<Product> products;
try
{
    products = new CsvProductReader().Read(path);
}
catch (FileNotFoundException)
{
    Console.Error.WriteLine($"Nie znaleziono pliku: {path}");
    return 1;
}
catch (CsvHelperException ex)
{
    Console.Error.WriteLine($"Błąd formatu pliku CSV ({path}): {ex.Message}");
    return 2;
}

Console.WriteLine($"Wczytano rekordów: {products.Count}");
Console.WriteLine();

foreach (var p in products.Take(3))
{
    Console.WriteLine($"[{p.Sku}] {p.Nazwa}");
    Console.WriteLine($"  Producent: {p.Producent ?? "(brak)"}");
    Console.WriteLine($"  Opis:      {p.Opis ?? "(brak)"}");
    Console.WriteLine();
}

var incomplete = products.Where(p => p.HasMissingData).ToList();
if (incomplete.Count > 0)
{
    Console.WriteLine($"Uwaga: rekordy z brakującymi danymi: {incomplete.Count} " +
                      $"({string.Join(", ", incomplete.Select(p => p.Sku))})");
}

return 0;
