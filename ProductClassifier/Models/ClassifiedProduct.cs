namespace ProductClassifier.Models;

/// <summary>
/// Produkt razem z wynikiem jego klasyfikacji.
/// </summary>
public record ClassifiedProduct(Product Produkt, Classification Klasyfikacja)
{
    /// <summary>
    /// Decyzja aplikacji, nie modelu: rekord idzie do sprawdzenia przez człowieka,
    /// gdy model nie rozstrzygnął kategorii jednoznacznie albo gdy dane wejściowe
    /// były niekompletne — nawet jeśli model mimo to był pewny. Bez zastrzeżeń
    /// przechodzą tylko kompletne rekordy z pewnością wysoką.
    /// </summary>
    public bool DoWeryfikacji => PowodPewnosci is not null || Produkt.HasMissingData;

    /// <summary>
    /// Powody rozdzielone przecinkiem — rekord może trafić do weryfikacji
    /// z obu przyczyn naraz. Pusty, gdy rekord nie wymaga weryfikacji.
    /// </summary>
    public string Powod => string.Join(", ", ZbierzPowody());

    private string? PowodPewnosci => Klasyfikacja.Pewnosc switch
    {
        ConfidenceLevel.Niska => "niska pewność",
        ConfidenceLevel.Srednia => "średnia pewność",
        _ => null,
    };

    private IEnumerable<string> ZbierzPowody()
    {
        if (PowodPewnosci is { } pewnosc)
        {
            yield return pewnosc;
        }

        if (Produkt.HasMissingData)
        {
            yield return "niekompletne dane";
        }
    }
}
