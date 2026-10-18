using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using ProductClassifier.Models;

namespace ProductClassifier.Services;

/// <summary>
/// Zapisuje wyniki klasyfikacji do dwóch plików CSV: osobno rekordy przyjęte
/// i osobno te wymagające sprawdzenia przez człowieka. Każdy rekord trafia
/// dokładnie do jednego pliku.
/// </summary>
public class ClassificationCsvWriter
{
    private static readonly CsvConfiguration Config = new(CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
    };

    // Z BOM — bez niego Excel na Windowsie rozjeżdża polskie znaki.
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    public string SklasyfikowanePath { get; }

    public string DoWeryfikacjiPath { get; }

    /// <param name="inputPath">Plik wejściowy — jego nazwa wyznacza nazwy wyników.</param>
    /// <param name="outputDirectory">Katalog, do którego trafiają oba pliki.</param>
    public ClassificationCsvWriter(string inputPath, string outputDirectory)
    {
        var nazwa = Path.GetFileNameWithoutExtension(inputPath);
        SklasyfikowanePath = Path.Combine(outputDirectory, $"{nazwa}.sklasyfikowane.csv");
        DoWeryfikacjiPath = Path.Combine(outputDirectory, $"{nazwa}.do-weryfikacji.csv");
    }

    /// <summary>
    /// Nadpisuje oba pliki. Plik do weryfikacji powstaje również pusty, z samym
    /// nagłówkiem — brak pliku nie powinien oznaczać "nie wiadomo".
    /// </summary>
    public void Write(
        IEnumerable<ClassifiedProduct> sklasyfikowane,
        IEnumerable<ReviewItem> doWeryfikacji)
    {
        WriteFile(SklasyfikowanePath, sklasyfikowane.Select(SklasyfikowanyRow.From));
        WriteFile(DoWeryfikacjiPath, doWeryfikacji.Select(DoWeryfikacjiRow.From));
    }

    private static void WriteFile<T>(string path, IEnumerable<T> rows)
    {
        using var writer = new StreamWriter(path, append: false, Utf8WithBom);
        using var csv = new CsvWriter(writer, Config);

        // Jawny nagłówek zamiast WriteRecords, żeby powstał także dla pustej listy.
        csv.WriteHeader<T>();
        csv.NextRecord();

        foreach (var row in rows)
        {
            csv.WriteRecord(row);
            csv.NextRecord();
        }
    }

    public sealed class SklasyfikowanyRow
    {
        [Index(0), Name("sku")] public string Sku { get; init; } = "";
        [Index(1), Name("nazwa")] public string Nazwa { get; init; } = "";
        [Index(2), Name("opis")] public string Opis { get; init; } = "";
        [Index(3), Name("producent")] public string Producent { get; init; } = "";
        [Index(4), Name("kategoria")] public string Kategoria { get; init; } = "";
        [Index(5), Name("pewnosc")] public string Pewnosc { get; init; } = "";
        [Index(6), Name("uzasadnienie")] public string Uzasadnienie { get; init; } = "";

        public static SklasyfikowanyRow From(ClassifiedProduct c) => new()
        {
            Sku = c.Produkt.Sku,
            Nazwa = c.Produkt.Nazwa,
            Opis = c.Produkt.Opis ?? "",
            Producent = c.Produkt.Producent ?? "",
            Kategoria = c.Klasyfikacja.Kategoria.Display(),
            Pewnosc = c.Klasyfikacja.Pewnosc.Display(),
            Uzasadnienie = c.Klasyfikacja.Uzasadnienie,
        };
    }

    public sealed class DoWeryfikacjiRow
    {
        [Index(0), Name("sku")] public string Sku { get; init; } = "";
        [Index(1), Name("nazwa")] public string Nazwa { get; init; } = "";
        [Index(2), Name("opis")] public string Opis { get; init; } = "";
        [Index(3), Name("producent")] public string Producent { get; init; } = "";
        [Index(4), Name("kategoria")] public string Kategoria { get; init; } = "";
        [Index(5), Name("pewnosc")] public string Pewnosc { get; init; } = "";
        [Index(6), Name("uzasadnienie")] public string Uzasadnienie { get; init; } = "";
        [Index(7), Name("powod")] public string Powod { get; init; } = "";

        // Przy nieudanej klasyfikacji trzy kolumny modelu zostają puste.
        public static DoWeryfikacjiRow From(ReviewItem item) => new()
        {
            Sku = item.Produkt.Sku,
            Nazwa = item.Produkt.Nazwa,
            Opis = item.Produkt.Opis ?? "",
            Producent = item.Produkt.Producent ?? "",
            Kategoria = item.Klasyfikacja?.Kategoria.Display() ?? "",
            Pewnosc = item.Klasyfikacja?.Pewnosc.Display() ?? "",
            Uzasadnienie = item.Klasyfikacja?.Uzasadnienie ?? "",
            Powod = item.Powod,
        };
    }
}
