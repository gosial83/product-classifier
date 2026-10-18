# ProductClassifier

Aplikacja konsolowa .NET 8 do klasyfikacji produktów (obecnie: kosmetyki do pielęgnacji brody) na podstawie danych z pliku CSV. Projekt demonstracyjny: klasyfikacja danych produktowych z użyciem LLM.

## Cel

- **Obecny etap:** wczytanie pliku CSV (`sku`, `nazwa`, `opis`, `producent`) do listy obiektów `Product`, wypisanie liczby rekordów i pierwszych trzech oraz ostrzeżenie o rekordach z brakującymi danymi.
- **Docelowo:** klasyfikacja produktów przez API modelu językowego z użyciem **tool callingu**. Zakres tej wersji:
  - **Wejście modelu:** `nazwa`, `opis`, `producent` jednego produktu. Pola puste przekazujemy jawnie jako brak, nie pomijamy ich.
  - **Wyjście modelu:** wywołanie **jednego** narzędzia, które zwraca:
    - `kategoria`, wyłącznie z zamkniętej listy: `olejek`, `balsam`, `szampon`, `wosk`, `inne`;
    - `pewnosc`: `wysoka`, `srednia` albo `niska`;
    - `uzasadnienie`: jedno zdanie.
  - Aplikacja waliduje odpowiedź (wartości spoza list albo brak pola to błąd, nie wynik) i dopiero wtedy ją zapisuje.
  - **Zasada nadrzędna:** model nigdy nie uzupełnia informacji, których nie ma w danych wejściowych (np. nie zgaduje producenta ani nie dopowiada składu). Przy niepewności zwraca pewność `niska`, a aplikacja oznacza taki rekord do weryfikacji ręcznej, zamiast traktować wynik jako pewny.
  - **Poza zakresem tej wersji (świadomie):** ekstrakcja dodatkowych atrybutów (np. pojemność, zapach, składniki, przeznaczenie). Nie rozszerzaj narzędzia o takie pola bez osobnej decyzji.

  Rekordy niekompletne (pusty opis lub producent) są w danych celowo, bo klasyfikator musi je obsłużyć świadomie, a nie po cichu.

## Struktura katalogów

```
product-classifier/
├── CLAUDE.md
├── ProductClassifier.sln
└── ProductClassifier/
    ├── ProductClassifier.csproj   # net8.0, Nullable + ImplicitUsings włączone, CsvHelper
    ├── Program.cs                 # top-level statements: wejście, wypisywanie wyników, kody wyjścia
    ├── Models/
    │   └── Product.cs             # record Product; Opis i Producent są nullable
    ├── Services/
    │   └── CsvProductReader.cs    # wczytywanie CSV (CsvHelper), puste pola → null
    └── Data/
        └── products.csv           # przykładowe dane, kopiowane do katalogu wyjściowego
```

## Uruchamianie

```bash
dotnet build ProductClassifier.sln
dotnet run --project ProductClassifier                 # domyślnie Data/products.csv
dotnet run --project ProductClassifier -- inny.csv     # własny plik
```

Kody wyjścia: `0` sukces, `1` brak pliku, `2` błąd formatu CSV.

## Konwencje kodu

- **Język:** komunikaty dla użytkownika i komentarze po polsku. Identyfikatory techniczne (klasy, metody, przestrzenie nazw) po angielsku, a właściwości odpowiadające kolumnom CSV po polsku (`Nazwa`, `Opis`, `Producent`).
- **Przestrzenie nazw** w stylu file-scoped (`namespace ProductClassifier.Services;`), zgodne ze ścieżką katalogu.
- **Modele** jako niezmienne `record`. Zmiany danych przez `with`, bez setterów.
- **Brak danych jest jawny:** pole opcjonalne ma typ nullable (`string?`), a pusty string z CSV jest normalizowany do `null` w warstwie wczytywania. Nie wprowadzaj wartości zastępczych typu `""` czy `"brak"` w modelu, bo `"(brak)"` to wyłącznie sprawa prezentacji.
- **Nullable reference types** są włączone. Kod ma się kompilować bez ostrzeżeń.
- **Obsługa błędów:** oczekiwane błędy (brak pliku, zły format) kończą się czytelnym komunikatem na `stderr` i niezerowym kodem wyjścia, bez stack trace'a.
- **CSV:** CsvHelper, separator przecinek, UTF-8, nagłówki dopasowywane bez względu na wielkość liter. Nie pisz własnego parsera.
- Komentarze tylko tam, gdzie wyjaśniają *dlaczego*, a nie *co*.
- Kodowanie plików UTF-8. Polskie znaki muszą działać w konsoli Windows (`Console.OutputEncoding = UTF8`).

## Zasady pracy

- **Przed większymi zmianami przedstaw plan i poczekaj na akceptację.** Dotyczy to nowych funkcji, zmian struktury, nowych zależności NuGet i integracji z API. Drobne poprawki (literówka, oczywisty błąd w jednej linii) można robić od razu.
- Po zmianach zbuduj projekt i uruchom go, a w odpowiedzi pokaż faktyczny wynik.
- Commituj tylko na wyraźną prośbę. Autor commitów jest ustawiony lokalnie w repo (`Małgorzata Łukasik`), nie zmieniaj go.
- Klucze API (przy integracji z modelem językowym) wyłącznie przez zmienne środowiskowe lub `dotnet user-secrets`, nigdy w repozytorium.
