namespace ProductClassifier.Models;

/// <summary>
/// Pojedynczy rekord produktowy wczytany z pliku CSV.
/// Opis i producent mogą być nieobecne w danych źródłowych, stąd typy nullable.
/// </summary>
public record Product(string Sku, string Nazwa, string? Opis, string? Producent)
{
    public bool HasMissingData =>
        string.IsNullOrWhiteSpace(Sku)
        || string.IsNullOrWhiteSpace(Nazwa)
        || Opis is null
        || Producent is null;
}
