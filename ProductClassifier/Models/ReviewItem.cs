namespace ProductClassifier.Models;

/// <summary>
/// Rekord skierowany do sprawdzenia przez człowieka, wraz z powodem.
/// Trafiają tu zarówno produkty sklasyfikowane niepewnie lub z niekompletnymi
/// danymi, jak i te, dla których klasyfikacja się nie powiodła — wtedy
/// <see cref="Klasyfikacja"/> jest null.
/// </summary>
public record ReviewItem(Product Produkt, Classification? Klasyfikacja, string Powod)
{
    public static ReviewItem FromClassified(ClassifiedProduct sklasyfikowany) =>
        new(sklasyfikowany.Produkt, sklasyfikowany.Klasyfikacja, sklasyfikowany.Powod);

    public static ReviewItem FromError(Product produkt, string komunikat) =>
        new(produkt, null, $"błąd klasyfikacji: {komunikat}");
}
