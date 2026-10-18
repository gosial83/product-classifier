# ProductClassifier

Aplikacja konsolowa .NET 8 do klasyfikacji produktów (obecnie: kosmetyki do pielęgnacji brody) na podstawie danych z pliku CSV. Projekt demonstracyjny: klasyfikacja danych produktowych z użyciem LLM.

## Cel

### Obecny etap (zaimplementowane)

**Wczytywanie:** plik CSV (`sku`, `nazwa`, `opis`, `producent`) do listy obiektów `Product`. Program wypisuje liczbę rekordów, pierwsze trzy i ostrzeżenie o rekordach z brakującymi danymi.

**Klasyfikacja** (flaga `--classify`) przez API Anthropic, model `claude-haiku-5-5`, z użyciem **tool callingu**:

- **Wejście modelu:** `nazwa`, `opis`, `producent` jednego produktu. Pola puste przekazywane są jawnie jako `(brak danych)`, nie pomijane. `sku` nie trafia do modelu — nie niesie informacji o produkcie.
- **Wyjście modelu:** wywołanie **jednego** narzędzia `zapisz_klasyfikacje`, wymuszonego przez `tool_choice`, które zwraca:
  - `kategoria`, wyłącznie z zamkniętej listy: `olejek`, `balsam`, `szampon`, `wosk`, `inne`;
  - `pewnosc`: `wysoka`, `srednia` albo `niska`;
  - `uzasadnienie`: jedno zdanie.
- Jedno wywołanie API na produkt, bez pętli agentowej — `tool_result` nie wraca do modelu, bo nie potrzebujemy jego komentarza. Narzędzie służy tu do wymuszenia ustrukturyzowanej odpowiedzi.
- **Walidacja** w jednym miejscu: `ClassificationTool.Parse`. Brak pola albo wartość spoza zamkniętej listy to błąd (`ClassificationException`), nie wynik — nie ma cichego fallbacku na `inne`. Żadna inna ścieżka nie tworzy obiektu `Classification`.
- **Zasada nadrzędna:** model nigdy nie uzupełnia informacji, których nie ma w danych wejściowych (nie zgaduje producenta ani nie dopowiada składu). Przy niepewności obniża `pewnosc`.

**Reguła weryfikacji ręcznej** — decyzja aplikacji, nie modelu, zapisana w `ClassifiedProduct`:

> Rekord trafia do weryfikacji, gdy pewność jest `niska` **lub** `srednia`, **albo** gdy `Product.HasMissingData` jest prawdą.

Bez zastrzeżeń przechodzą więc tylko kompletne rekordy z pewnością `wysoka`. Drugi człon istnieje, bo model bywa pewny mimo braków w danych — to przypadek, którego sam prompt nie wyłapie. `ClassifiedProduct.Powod` podaje przyczyny rozdzielone przecinkiem (`niska pewność`, `średnia pewność`, `niekompletne dane`).

**Zapis wyników:** dwa pliki CSV w katalogu bieżącym, nazwane od pliku wejściowego. Każdy rekord trafia dokładnie do jednego z nich.

| plik | zawartość | kolumny |
|---|---|---|
| `<nazwa>.sklasyfikowane.csv` | rekordy przyjęte bez zastrzeżeń | kolumny wejściowe + `kategoria`, `pewnosc`, `uzasadnienie` |
| `<nazwa>.do-weryfikacji.csv` | rekordy do sprawdzenia przez człowieka oraz te, dla których klasyfikacja się nie powiodła | jw. + `powod` |

Pliki są zapisywane w UTF-8 **z BOM**, żeby Excel na Windowsie nie rozjeżdżał polskich znaków, i nadpisywane przy każdym uruchomieniu. Plik do weryfikacji powstaje także pusty, z samym nagłówkiem — brak pliku nie powinien oznaczać „nie wiadomo". Oba wzorce są w `.gitignore`.

Rekordy niekompletne (pusty opis lub producent) są w przykładowych danych celowo, bo klasyfikator musi je obsłużyć świadomie, a nie po cichu.

### Poza zakresem (świadomie)

- **Ekstrakcja dodatkowych atrybutów** (pojemność, zapach, składniki, przeznaczenie). Nie rozszerzaj narzędzia o takie pola bez osobnej decyzji.
- **Przetwarzanie wsadowe i równoległe.** Produkty klasyfikowane są sekwencyjnie, jeden po drugim.
- **Testy.** Projektu testów jeszcze nie ma; najbardziej opłacalne byłyby dla `ClassificationTool.Parse` i `ClassifiedProduct`, bo to czyste funkcje niewymagające sieci.

## Struktura katalogów

```
product-classifier/
├── CLAUDE.md
├── ProductClassifier.sln
└── ProductClassifier/
    ├── ProductClassifier.csproj       # net8.0, Nullable + ImplicitUsings, CsvHelper + Anthropic
    ├── Program.cs                     # top-level statements: wejście, wypisywanie, kody wyjścia
    ├── Models/
    │   ├── Product.cs                 # record Product; Opis i Producent są nullable
    │   ├── ProductCategory.cs         # enum: Olejek, Balsam, Szampon, Wosk, Inne
    │   ├── ConfidenceLevel.cs         # enum: Wysoka, Srednia, Niska
    │   ├── Classification.cs          # wynik od modelu; bez decyzji o weryfikacji
    │   ├── ClassifiedProduct.cs       # Product + Classification; reguła weryfikacji i Powod
    │   ├── ReviewItem.cs              # rekord do weryfikacji wraz z powodem
    │   └── ClassificationText.cs      # nazwy do wyświetlenia (z polskimi znakami)
    ├── Services/
    │   ├── CsvProductReader.cs        # wczytywanie CSV, puste pola → null
    │   ├── ClassificationPrompt.cs    # treść promptu i budowa wiadomości użytkownika
    │   ├── ClassificationTool.cs      # definicja narzędzia + jedyna bramka walidacji
    │   ├── ClaudeProductClassifier.cs # wywołanie API, jedno na produkt
    │   ├── ClassificationCsvWriter.cs # zapis obu plików wynikowych
    │   └── ClassificationException.cs
    └── Data/
        └── products.csv               # przykładowe dane, kopiowane do katalogu wyjściowego
```

## Uruchamianie

```bash
dotnet build ProductClassifier.sln
dotnet run --project ProductClassifier                 # samo wczytanie, domyślnie Data/products.csv
dotnet run --project ProductClassifier -- inny.csv     # własny plik
dotnet run --project ProductClassifier -- --classify   # wczytanie + klasyfikacja + zapis wyników
```

Klasyfikacja wymaga klucza w zmiennej środowiskowej `ANTHROPIC_API_KEY` i kosztuje — każdy produkt to jedno wywołanie API.

Pliki wynikowe trafiają do **katalogu bieżącego**, nie obok pliku wejściowego. Domyślne wejście leży w katalogu wyjściowym builda, więc zapis „obok" oznaczałby `bin/`, gdzie wyniki ginęłyby przy `dotnet clean`.

Kody wyjścia: `0` sukces, `1` brak pliku, `2` błąd formatu CSV, `3` brak `ANTHROPIC_API_KEY`, `4` co najmniej jeden produkt nie został sklasyfikowany, `5` nieudany zapis wyników.

## Konwencje kodu

- **Język:** komunikaty dla użytkownika i komentarze po polsku. Identyfikatory techniczne (klasy, metody, przestrzenie nazw) po angielsku, a właściwości odpowiadające kolumnom CSV po polsku (`Nazwa`, `Opis`, `Producent`).
- **Przestrzenie nazw** w stylu file-scoped (`namespace ProductClassifier.Services;`), zgodne ze ścieżką katalogu.
- **Modele** jako niezmienne `record`. Zmiany danych przez `with`, bez setterów.
- **Brak danych jest jawny:** pole opcjonalne ma typ nullable (`string?`), a pusty string z CSV jest normalizowany do `null` w warstwie wczytywania. Nie wprowadzaj wartości zastępczych typu `""` czy `"brak"` w modelu, bo `"(brak)"` to wyłącznie sprawa prezentacji.
- **Nullable reference types** są włączone. Kod ma się kompilować bez ostrzeżeń.
- **Obsługa błędów:** oczekiwane błędy (brak pliku, zły format) kończą się czytelnym komunikatem na `stderr` i niezerowym kodem wyjścia, bez stack trace'a.
- **CSV:** CsvHelper, separator przecinek, nagłówki dopasowywane bez względu na wielkość liter. Odczyt w UTF-8, zapis w UTF-8 z BOM. Nie pisz własnego parsera.
- **Prompt** mieszka w `ClassificationPrompt.cs`, nie w kodzie wywołującym API — zmienia się najczęściej i ma być łatwy do przejrzenia w diffie.
- **Wartości wymieniane z API są bez polskich znaków** (`pewnosc`, `srednia`), bo trafiają do schematu JSON. Polskie znaki pojawiają się dopiero w warstwie prezentacji (`ClassificationText`).
- **Model** jest ustawiony w `ClaudeProductClassifier.ModelId`. Jego zmiana to decyzja do uzgodnienia, nie refaktor — `claude-haiku-5-5` został wybrany między innymi dlatego, że przyjmuje wymuszone `tool_choice`, którego nowsze modele Opus i Sonnet odrzucają błędem 400.
- Komentarze tylko tam, gdzie wyjaśniają *dlaczego*, a nie *co*.
- Kodowanie plików UTF-8. Polskie znaki muszą działać w konsoli Windows (`Console.OutputEncoding = UTF8`).

## Zasady pracy

- **Przed większymi zmianami przedstaw plan i poczekaj na akceptację.** Dotyczy to nowych funkcji, zmian struktury, nowych zależności NuGet i integracji z API. Drobne poprawki (literówka, oczywisty błąd w jednej linii) można robić od razu.
- Po zmianach zbuduj projekt i uruchom go, a w odpowiedzi pokaż faktyczny wynik.
- Commituj tylko na wyraźną prośbę. Autor commitów jest ustawiony lokalnie w repo (`Małgorzata Łukasik`), nie zmieniaj go.
- Klucze API wyłącznie przez zmienne środowiskowe (`ANTHROPIC_API_KEY`) lub `dotnet user-secrets`, nigdy w repozytorium.
- **Zmiany promptu i reguły weryfikacji pokazuj do akceptacji przed napisaniem kodu.** To decyzje produktowe, nie techniczne, i były już kilka razy korygowane.
