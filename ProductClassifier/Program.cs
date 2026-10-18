using System.Text;
using Anthropic;
using CsvHelper;
using ProductClassifier.Models;
using ProductClassifier.Services;

Console.OutputEncoding = Encoding.UTF8;

var classify = args.Contains("--classify");
var positional = args.Where(a => !a.StartsWith("--")).ToArray();

var path = positional.Length > 0
    ? positional[0]
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

if (!classify)
{
    Console.WriteLine();
    Console.WriteLine("Aby sklasyfikować produkty, uruchom z flagą --classify.");
    return 0;
}

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
{
    Console.Error.WriteLine("Brak zmiennej środowiskowej ANTHROPIC_API_KEY.");
    return 3;
}

Console.WriteLine();
Console.WriteLine($"Klasyfikacja ({ClaudeProductClassifier.ModelId})");
Console.WriteLine();

var classifier = new ClaudeProductClassifier(new AnthropicClient());
var przyjete = new List<ClassifiedProduct>();
var doWeryfikacji = new List<ReviewItem>();
var bledy = new List<string>();

foreach (var p in products)
{
    Console.WriteLine($"[{p.Sku}] {p.Nazwa}");
    try
    {
        var wynik = new ClassifiedProduct(p, await classifier.ClassifyAsync(p));

        Console.WriteLine($"  Kategoria:    {wynik.Klasyfikacja.Kategoria.Display()} " +
                          $"(pewność: {wynik.Klasyfikacja.Pewnosc.Display()})");
        Console.WriteLine($"  Uzasadnienie: {wynik.Klasyfikacja.Uzasadnienie}");

        if (wynik.DoWeryfikacji)
        {
            Console.WriteLine($"  -> weryfikacja ręczna ({wynik.Powod})");
            doWeryfikacji.Add(ReviewItem.FromClassified(wynik));
        }
        else
        {
            przyjete.Add(wynik);
        }
    }
    catch (ClassificationException ex)
    {
        Console.WriteLine($"  BŁĄD: {ex.Message}");
        doWeryfikacji.Add(ReviewItem.FromError(p, ex.Message));
        bledy.Add(p.Sku);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  BŁĄD wywołania API: {ex.Message}");
        doWeryfikacji.Add(ReviewItem.FromError(p, ex.Message));
        bledy.Add(p.Sku);
    }

    Console.WriteLine();
}

Console.WriteLine($"Przyjęte bez zastrzeżeń: {przyjete.Count} z {products.Count}");

if (doWeryfikacji.Count > 0)
{
    Console.WriteLine($"Do weryfikacji ręcznej: {doWeryfikacji.Count}");
    foreach (var w in doWeryfikacji)
    {
        Console.WriteLine($"  - {w.Produkt.Sku} {w.Produkt.Nazwa} ({w.Powod})");
    }
}

var csvWriter = new ClassificationCsvWriter(path, Directory.GetCurrentDirectory());
try
{
    csvWriter.Write(przyjete, doWeryfikacji);
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Nie udało się zapisać wyników: {ex.Message}");
    return 5;
}

Console.WriteLine();
Console.WriteLine("Zapisano:");
Console.WriteLine($"  {csvWriter.SklasyfikowanePath} ({przyjete.Count})");
Console.WriteLine($"  {csvWriter.DoWeryfikacjiPath} ({doWeryfikacji.Count})");

return bledy.Count > 0 ? 4 : 0;
