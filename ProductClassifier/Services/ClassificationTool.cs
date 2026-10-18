using System.Text.Json;
using Anthropic.Models.Messages;
using ProductClassifier.Models;

namespace ProductClassifier.Services;

/// <summary>
/// Definicja narzędzia przekazywanego modelowi oraz jedyna ścieżka, którą
/// odpowiedź modelu zamienia się w obiekt <see cref="Classification"/>.
/// </summary>
public static class ClassificationTool
{
    public const string Name = "zapisz_klasyfikacje";

    // Wartości na drucie są bez polskich znaków — trafiają do schematu JSON.
    private static readonly string[] Kategorie = ["olejek", "balsam", "szampon", "wosk", "inne"];
    private static readonly string[] Pewnosci = ["wysoka", "srednia", "niska"];

    public static ToolUnion Definition => new(new Tool
    {
        Name = Name,
        Description =
            "Zapisuje kategorię produktu kosmetycznego do pielęgnacji brody. "
            + "Wywołaj dokładnie raz dla ocenianego produktu. Opieraj się wyłącznie "
            + "na przekazanych polach; nie uzupełniaj brakujących informacji.",
        InputSchema = new InputSchema
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["kategoria"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    @enum = Kategorie,
                    description = "Kategoria produktu wybrana według jego głównej funkcji.",
                }),
                ["pewnosc"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    @enum = Pewnosci,
                    description = "Poziom pewności. \"niska\" oznacza, że dane nie wystarczają "
                                + "i rekord trafi do weryfikacji ręcznej.",
                }),
                ["uzasadnienie"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Jedno zdanie: które fragmenty nazwy lub opisu wskazują "
                                + "na tę kategorię. Przy niskiej pewności — czego zabrakło.",
                }),
            },
            Required = ["kategoria", "pewnosc", "uzasadnienie"],
        },
    });

    /// <summary>
    /// Waliduje argumenty wywołania narzędzia. Brak pola albo wartość spoza
    /// zamkniętej listy to błąd, nie wynik — nie ma cichego fallbacku na "inne".
    /// </summary>
    /// <exception cref="ClassificationException"/>
    public static Classification Parse(IReadOnlyDictionary<string, JsonElement> input) =>
        new(
            ParseKategoria(ReadString(input, "kategoria")),
            ParsePewnosc(ReadString(input, "pewnosc")),
            ReadString(input, "uzasadnienie"));

    private static string ReadString(IReadOnlyDictionary<string, JsonElement> input, string key)
    {
        if (!input.TryGetValue(key, out var element))
        {
            throw new ClassificationException($"Odpowiedź modelu nie zawiera pola \"{key}\".");
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new ClassificationException(
                $"Pole \"{key}\" ma typ {element.ValueKind}, oczekiwano tekstu.");
        }

        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ClassificationException($"Pole \"{key}\" jest puste.");
        }

        return value.Trim();
    }

    // Jawny switch zamiast Enum.Parse: Enum.Parse przyjęłoby też wartości,
    // których nie ma w schemacie (np. inną wielkość liter albo liczbę).
    private static ProductCategory ParseKategoria(string value) => value switch
    {
        "olejek" => ProductCategory.Olejek,
        "balsam" => ProductCategory.Balsam,
        "szampon" => ProductCategory.Szampon,
        "wosk" => ProductCategory.Wosk,
        "inne" => ProductCategory.Inne,
        _ => throw new ClassificationException(
            $"Nieznana kategoria \"{value}\". Dozwolone: {string.Join(", ", Kategorie)}."),
    };

    private static ConfidenceLevel ParsePewnosc(string value) => value switch
    {
        "wysoka" => ConfidenceLevel.Wysoka,
        "srednia" => ConfidenceLevel.Srednia,
        "niska" => ConfidenceLevel.Niska,
        _ => throw new ClassificationException(
            $"Nieznany poziom pewności \"{value}\". Dozwolone: {string.Join(", ", Pewnosci)}."),
    };
}
