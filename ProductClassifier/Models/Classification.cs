namespace ProductClassifier.Models;

/// <summary>
/// Wynik klasyfikacji jednego produktu. Powstaje wyłącznie w
/// <see cref="Services.ClassificationTool.Parse"/>, po walidacji odpowiedzi modelu.
/// </summary>
/// <remarks>
/// Celowo nie ma tu właściwości "do weryfikacji" — to, czy rekord wymaga
/// sprawdzenia przez człowieka, zależy także od kompletności danych
/// wejściowych, więc decyduje o tym <see cref="ClassifiedProduct"/>.
/// </remarks>
public record Classification(
    ProductCategory Kategoria,
    ConfidenceLevel Pewnosc,
    string Uzasadnienie);
