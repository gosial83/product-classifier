namespace ProductClassifier.Models;

/// <summary>
/// Nazwy do wyświetlenia na konsoli. Celowo oddzielone od wartości
/// przesyłanych do API, które są bez polskich znaków.
/// </summary>
public static class ClassificationText
{
    public static string Display(this ProductCategory kategoria) => kategoria switch
    {
        ProductCategory.Olejek => "olejek",
        ProductCategory.Balsam => "balsam",
        ProductCategory.Szampon => "szampon",
        ProductCategory.Wosk => "wosk",
        ProductCategory.Inne => "inne",
        _ => kategoria.ToString(),
    };

    public static string Display(this ConfidenceLevel pewnosc) => pewnosc switch
    {
        ConfidenceLevel.Wysoka => "wysoka",
        ConfidenceLevel.Srednia => "średnia",
        ConfidenceLevel.Niska => "niska",
        _ => pewnosc.ToString(),
    };
}
