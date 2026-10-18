namespace ProductClassifier.Services;

/// <summary>
/// Odpowiedź modelu nie nadaje się do użycia: brakuje pola, wartość jest spoza
/// zamkniętej listy albo model w ogóle nie wywołał narzędzia.
/// </summary>
public class ClassificationException : Exception
{
    public ClassificationException(string message) : base(message) { }

    public ClassificationException(string message, Exception inner) : base(message, inner) { }
}
