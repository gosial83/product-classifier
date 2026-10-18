namespace ProductClassifier.Models;

/// <summary>
/// Produkt razem z wynikiem jego klasyfikacji.
/// </summary>
public record ClassifiedProduct(Product Produkt, Classification Klasyfikacja)
{
    private bool NiskaPewnosc => Klasyfikacja.Pewnosc == ConfidenceLevel.Niska;

    /// <summary>
    /// Decyzja aplikacji, nie modelu: rekord idzie do sprawdzenia przez człowieka,
    /// gdy model nie rozstrzygnął kategorii albo gdy dane wejściowe były
    /// niekompletne — nawet jeśli model mimo to był pewny.
    /// </summary>
    public bool DoWeryfikacji => NiskaPewnosc || Produkt.HasMissingData;

    public string Powod => (NiskaPewnosc, Produkt.HasMissingData) switch
    {
        (true, true) => "niska pewność, niekompletne dane",
        (true, false) => "niska pewność",
        (false, true) => "niekompletne dane",
        _ => "",
    };
}
