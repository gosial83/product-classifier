namespace ProductClassifier.Models;

/// <summary>
/// Zamknięta lista kategorii. Model może zwrócić wyłącznie te wartości;
/// cokolwiek innego jest błędem odpowiedzi, a nie nową kategorią.
/// </summary>
public enum ProductCategory
{
    Olejek,
    Balsam,
    Szampon,
    Wosk,
    Inne,
}
