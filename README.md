# ProductClassifier

Aplikacja konsolowa .NET 8, która kategoryzuje produkty z pliku CSV za pomocą modelu językowego i rozdziela wynik na rekordy gotowe do użycia oraz takie, które musi obejrzeć człowiek — bo model sam z siebie nie sygnalizuje, kiedy zgaduje.

Dane przykładowe to kosmetyki do pielęgnacji brody.

## Uruchomienie

Wymagania: .NET SDK 8 i klucz API Anthropic w zmiennej środowiskowej `ANTHROPIC_API_KEY`.

```powershell
setx ANTHROPIC_API_KEY "twoj-klucz"
```

`setx` zapisuje zmienną na stałe, ale widzą ją dopiero nowo otwarte terminale — w bieżącym trzeba ustawić ją jeszcze raz przez `$env:ANTHROPIC_API_KEY = "twoj-klucz"`.

```bash
dotnet build ProductClassifier.sln

# samo wczytanie pliku, bez wywołań API
dotnet run --project ProductClassifier

# wczytanie, klasyfikacja i zapis wyników
dotnet run --project ProductClassifier -- --classify

# własny plik wejściowy
dotnet run --project ProductClassifier -- katalog.csv --classify
```

Plik wejściowy ma kolumny `sku`, `nazwa`, `opis`, `producent`. Klasyfikacja to jedno wywołanie API na produkt, więc kosztuje — bez flagi `--classify` program niczego nie wysyła.

Wyniki trafiają do katalogu bieżącego jako dwa pliki CSV nazwane od pliku wejściowego: `<nazwa>.sklasyfikowane.csv` z rekordami przyjętymi i `<nazwa>.do-weryfikacji.csv` z resztą, wraz z kolumną `powod`.

### Przykładowy wynik

```
Klasyfikacja (claude-haiku-5-5)

[BRD-001] Olejek do brody Cedr & Bergamotka
  Kategoria:    olejek (pewność: wysoka)
  Uzasadnienie: Nazwa "Olejek do brody" oraz opis "lekki olejek arganowo-jojoba"
                jednoznacznie wskazują na bezwodny preparat olejowy.

[BRD-003] Szampon do brody Mięta
  Kategoria:    szampon (pewność: wysoka)
  Uzasadnienie: Nazwa jednoznacznie wskazuje produkt myjący; opis jest
                niedostępny, ale nazwa wystarcza do klasyfikacji.
  -> weryfikacja ręczna (niekompletne dane)

Przyjęte bez zastrzeżeń: 3 z 6
Do weryfikacji ręcznej: 3
  - BRD-003 Szampon do brody Mięta (niekompletne dane)
  - BRD-005 Odżywka do brody Aloes (średnia pewność)
  - BRD-006 Serum do brody na porost (średnia pewność)

Zapisano:
  products.sklasyfikowane.csv (3)
  products.do-weryfikacji.csv (3)
```

## Jak to działa

**Wymuszony tool calling zamiast parsowania tekstu.** Model dostaje jedno narzędzie z zamkniętym schematem JSON i parametr `tool_choice`, który nie zostawia mu wyboru — musi je wywołać. Odpowiedź przychodzi jako ustrukturyzowane argumenty, a nie proza do rozebrania wyrażeniem regularnym.

**Walidacja w jednym miejscu.** Argumenty przechodzą przez `ClassificationTool.Parse` i tylko tamtędy. Brakujące pole albo wartość spoza listy kończy się wyjątkiem, nie cichym przypisaniem do kategorii `inne`. Żadna inna ścieżka w kodzie nie tworzy obiektu wyniku.

**O weryfikacji decyduje aplikacja, nie model.** Rekord trafia do sprawdzenia przez człowieka, gdy pewność jest niska lub średnia, **albo** gdy dane wejściowe były niekompletne — nawet jeśli model mimo to był pewny.

## Decyzje projektowe

**Dlaczego Haiku.** Zadanie jest proste i powtarzalne: krótki tekst na wejściu, jedna etykieta na wyjściu. Najtańszy model z rodziny wystarcza, a przy katalogu liczonym w tysiącach pozycji różnica w rachunku jest realna. Doszedł argument techniczny: `claude-haiku-5-5` przyjmuje wymuszone `tool_choice`, które nowsze modele Opus i Sonnet odrzucają błędem 400.

**Dlaczego nie ma pętli agentowej.** Narzędzie nie jest tu funkcją, którą model wywołuje, żeby coś sobie pobrać — służy wyłącznie do wymuszenia struktury odpowiedzi. Nie ma więc po co odsyłać `tool_result` ani prowadzić rozmowy dalej. Jeden produkt to jedno żądanie i jedna odpowiedź, co oszczędza połowę wywołań i usuwa całą klasę błędów związanych ze stanem konwersacji.

**Dlaczego model nie decyduje o skierowaniu do weryfikacji.** Model ocenia własną pewność i robi to sensownie, ale nie wie, czego zabrakło w danych wejściowych — widzi tylko to, co dostał. W praktyce potrafi z wysoką pewnością sklasyfikować rekord, w którym brakuje opisu i producenta, i z punktu widzenia samej kategorii ma rację. To, czy taki rekord nadaje się do dalszego użytku, jest decyzją procesu, nie modelu, więc zapadła w kodzie.

**Dlaczego zamknięta lista kategorii.** Otwarta lista daje synonimy („olejek", „olej", „oliwka") i etykiety wymyślone na poczekaniu, czyli wynik, którego nie da się zagregować. Lista wpisana w schemat JSON jest egzekwowana po obu stronach: model widzi ją w definicji narzędzia, a aplikacja odrzuca wszystko spoza niej.

## Czego ten projekt nie robi

Zakres jest świadomie wąski — to demonstracja jednego wzorca, nie produkt.

- **Testów automatycznych nie ma.** Najbardziej opłacalne byłyby dla walidacji odpowiedzi i reguły weryfikacji, bo to czyste funkcje niewymagające sieci.
- **Przetwarzania wsadowego nie ma.** Produkty idą sekwencyjnie, jeden po drugim. Przy większym katalogu właściwym narzędziem jest Batch API albo równoległość z ograniczeniem liczby żądań.
- **Ekstrakcji dodatkowych atrybutów nie ma.** Pojemność, zapach czy skład dałoby się wyciągnąć tym samym mechanizmem, ale każde dodatkowe pole to nowy powód do halucynacji, więc narzędzie zwraca dokładnie trzy.
- **Dużych zbiorów nie obsługuje.** Wszystko wczytuje się do pamięci i zapisuje dopiero na końcu; przy setkach tysięcy rekordów potrzebny byłby strumieniowy zapis i wznawianie po przerwaniu.

Szczegóły konwencji i struktury katalogów: [CLAUDE.md](CLAUDE.md).
