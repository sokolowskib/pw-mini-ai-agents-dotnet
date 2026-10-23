# Raport — Laboratorium 03

**Zespół:** Krzysztof  
**Data:** 23.10.2026  

## Środowisko

| Parametr | Wartość |
|----------|---------|
| Wersja Ollama | 0.40.5 |
| Sprzęt (CPU/GPU, RAM) | Apple MacBook Air M5, 16 GB RAM |
| Testowane modele | `gemma3:4b` (3.4 GB), `llama3.2:latest` (2.0 GB) |
| Liczba dostępnych notatek | 170 (`notes.csv`), zbiór walidacyjny: 10 (`examples.csv`) |

## EXP-00 — Sposób oceny wyników

Jak oceniamy poprawność odpowiedzi modelu? Jakie metryki przyjmujemy i dlaczego?

| Metryka | Definicja |
|---------|-----------|
| **Poprawny JSON (`ValidJson`)** | Liczba (lub odsetek) odpowiedzi, które dają się zdeserializować do obiektu JSON i posiadają wymagane pola `floor` (`int` lub `null`) oraz `elevator` (`bool` lub `null`). |
| **Floor accuracy** | Odsetek wszystkich notatek, dla których model poprawnie wyodrębnił docelowe piętro (`floor`). |
| **Elevator accuracy** | Odsetek wszystkich notatek, dla których model poprawnie określił informację o windzie (`elevator`: `true`, `false` lub `null`). |
| **Exact match** | Odsetek notatek, dla których model jednocześnie poprawnie rozpoznał **oba** pola (`floor` i `elevator`). |
| **Średni czas (`AvgMs`)** | Średni czas generowania pojedynczej odpowiedzi przez model (w milisekundach). |

**Dlaczego taki zestaw metryk?**  
Poleganie wyłącznie na jednej zagregowanej mierze (`Exact match`) ukrywa przyczynę błędów. Rozbicie oceny na `Floor accuracy`, `Elevator accuracy` oraz `ValidJson` pozwala od razu zauważyć, czy model ma problem ze składnią JSON, z rozpoznawaniem liczb/pięter, czy też z nadinterpretacją braku wzmianki o windzie (wpisywanie `false` lub `true` zamiast `null`).

---

## EXP-01 — Porównanie modeli

| Model | Poprawny JSON (surowy / z parserem) | Floor accuracy | Elevator accuracy | Exact match | Średni czas [ms] |
|-------|-------------------------------------|----------------|-------------------|-------------|------------------|
| `gemma3:4b` | 10/10 | 80% | 50% | 40% | 928 ms |
| `llama3.2:latest` | 9/10 (10/10 po domknięciu `}`) | 70% | 50% | 30% | 498 ms |

**Exact match** oznacza odsetek notatek, dla których model poprawnie rozpoznał jednocześnie oba pola: `floor` i `elevator`.

**Wnioski:**
1. **Jakość ekstrakcji vs czas:** Większy model `gemma3:4b` osiąga wyższą trafność rozpoznawania piętra (`80%` vs `70%`, poprawnie rozumie m.in. „Parter” jako `0` w `NOTE-003`) i wyższy `Exact match` (`40%` vs `30%`), ale jest niemal dwukrotnie wolniejszy (`928 ms` vs `498 ms`) od mniejszego modelu `llama3.2:latest`.
2. **Wspólny problem obu modeli w zero-shot:** Oba modele dobrze radzą sobie z piętrem (`70–80%`), ale słabo z windą (`50%`) — gdy w notatce nie ma ani słowa o windzie (np. `NOTE-003`, `NOTE-004`, `NOTE-009`), modele zamiast zwrócić `null` zgadują `false` lub `true`.

---

## EXP-02 — Prompting

| Model | Wariant | Floor accuracy | Elevator accuracy | Exact match | Średni czas [ms] |
|-------|---------|----------------|-------------------|-------------|------------------|
| `gemma3:4b` | zero-shot | 80% | 50% | 40% | 715 ms |
| `gemma3:4b` | **one-shot** | **90%** | **90%** | **80%** | 718 ms |
| `gemma3:4b` | few-shot | 80% | 90% | 70% | 738 ms |
| `llama3.2:latest` | zero-shot | 70% | 50% | 30% | 358 ms |
| `llama3.2:latest` | **one-shot** | **90%** | **60%** | **50%** | 362 ms |
| `llama3.2:latest` | few-shot | 80% | 60% | 40% | 336 ms |

**Wnioski:**
1. **Ogromny skok jakości przy one-shot:** Dodanie zaledwie jednego przykładu (`EX-001`), w którym brak informacji o windzie został jawnie oznaczony jako `"elevator": null`, podniosło w modelu `gemma3:4b` metrykę `Elevator accuracy` z `50%` do `90%`, a `Exact match` dwukrotnie — z `40%` do **`80%`**. W modelu `llama3.2:latest` `Floor accuracy` wzrosło z `70%` do `90%`, a `Exact match` z `30%` do `50%`.
2. **One-shot vs Few-shot:** Wariant `one-shot` wypadł nieco lepiej niż `few-shot` dla obu modeli (`80%` vs `70%` dla Gemmy oraz `50%` vs `40%` dla Llamy). Pokazuje to, że więcej przykładów nie zawsze oznacza lepszy wynik — pojedynczy, dobrze dobrany przykład pokazujący wartość `null` wystarczył modelowi do zrozumienia konwencji bez rozpraszania jego uwagi dodatkowymi przypadkami.
3. Czas odpowiedzi między `zero-shot`, `one-shot` i `few-shot` zmienił się minimalnie (kilkanaście milisekund).

---

## EXP-03 — Temperature

| Model | Temperature | Poprawny JSON | Floor accuracy | Elevator accuracy | Exact match | Średni czas [ms] | Powtarzalność odpowiedzi |
|-------|-------------|---------------|----------------|-------------------|-------------|------------------|--------------------------|
| `gemma3:4b` | 0.0 | 50/50 | 80% | 50% | 40% | 572 ms | Pełna (10/10 powtarzalnych notatek) |
| `gemma3:4b` | 0.5 | 50/50 | 80% | 54% | 44% | 566 ms | Pojedyncze wahania między uruchomieniami |
| `gemma3:4b` | 1.0 | 50/50 | 80% | 56% | 46% | 582 ms | Niższa powtarzalność (losowe zmiany `null`/`false`) |
| `llama3.2:latest` | 0.0 | 50/50 | 70% | 50% | 30% | 316 ms | Pełna (10/10 powtarzalnych notatek) |
| `llama3.2:latest` | 0.5 | 50/50 | 72% | 44% | 32% | 299 ms | Wahania odpowiedzi dla trudniejszych notatek |
| `llama3.2:latest` | 1.0 | **48/50** | 70% | 46% | 32% | 284 ms | Spadek stabilności + 2 uszkodzone JSON-y |

**Wnioski:**
1. **Deterministyczność przy `temperature = 0.0`:** Dla obu modeli temperatura `0.0` daje w 100% powtarzalne odpowiedzi przy każdym z 5 powtórzeń.
2. **Wpływ wyższej temperatury na poprawność i stabilność:** Zwiększenie temperatury do `0.5` i `1.0` wprowadza niedeterminizm — dla części notatek model raz zgaduje `null`, a raz `false` (stąd pozorne drobne wahania `Exact match` o kilka punktów procentowych w `gemma3:4b`). Co istotne, przy `temperature = 1.0` model `llama3.2:latest` zaczął generować uszkodzone struktury JSON (`48/50` poprawnych).
3. **Zastosowanie w systemach agentowych:** W zadaniach ekstrakcji danych i integracji z kodem zawsze należy stosować `temperature = 0.0`, ponieważ priorytetem jest przewidywalność, powtarzalność i stabilność formatu wyjściowego.

---

## EXP-04 — Structured output — JSON i walidacja

| Model | Wariant | Poprawny JSON (surowy / z parserem) | Floor accuracy | Elevator accuracy | Exact match | Średni czas [ms] |
|-------|---------|-------------------------------------|----------------|-------------------|-------------|------------------|
| `gemma3:4b` | prompt | 0/10 (10/10 z `CleanJson`) | 80% | 50% | 40% | 713 ms |
| `gemma3:4b` | **json-schema** | **10/10 (natywnie czysty JSON)** | **80%** | **60%** | **50%** | **624 ms** |
| `llama3.2:latest` | prompt | 9/10 (10/10 z `CleanJson`) | 70% | 50% | 30% | 356 ms |
| `llama3.2:latest` | **json-schema** | **10/10 (natywnie czysty JSON)** | **70%** | **50%** | **30%** | **364 ms** |

**Wnioski:**
1. **Gwarancja poprawnej składni:** W wariancie `prompt` model `gemma3:4b` otacza JSON blokiem Markdown ```` ```json ... ``` ```` (co bez dodatkowego czyszczenia tekstu daje `0/10`), a `llama3.2:latest` potrafi pominąć zamykający nawias `}` (`9/10`). Włączenie `json-schema` sprawia, że oba modele w `10/10` przypadkach zwracają surowy, w pełni poprawny obiekt JSON zgodny z typami pól (`integer|null`, `boolean|null`).
2. **Wpływ na jakość i czas:** Dla `gemma3:4b` tryb `json-schema` przyspieszył generowanie (`624 ms` vs `713 ms`, bo model nie generuje zbędnych znaczników Markdown) i podniósł `Exact match` z `40%` do `50%`.
3. **Walidacja po stronie aplikacji nadal jest konieczna:** `json-schema` gwarantuje poprawność składniową i typologiczną (np. że `floor` to liczba całkowita lub `null`), ale nie gwarantuje poprawności semantycznej (np. wpisania numeru mieszkania jako piętra).

---

## Najtrudniejsze notatki

Które notatki model najczęściej interpretował błędnie i dlaczego?

1. **`NOTE-006`: `"Mieszkanie 15, domofon 15"` (oczekiwano: `floor=null, elevator=null`)**  
   Oba modele niemal zawsze zwracały `floor=15`. Modele widząc liczbę w krótkiej notatce adresowej przypisują ją do szukanego pola `floor`, myląc numer lokalu/domofonu z piętrem.
2. **`NOTE-007`: `"4 piętro, winda tylko do 3"` (oczekiwano: `floor=4, elevator=true`)**  
   Oba modele poprawnie rozpoznawały `floor=4`, ale zwracały `elevator=false`. Z punktu widzenia logiki potocznej „winda nie dojeżdża na 4 piętro = brak windy na 4 piętro”, podczas gdy specyfikacja zadania definiowała `elevator: true` jako sam fakt fizycznej obecności windy w budynku.
3. **`NOTE-010`: `"Winda nie działa, dostawa na 5p"` (oczekiwano: `floor=5, elevator=true`)**  
   Analogiczny problem semantyczny: modele interpretują niedziałającą windę jako `elevator=false` (brak dostępnej windy), zamiast `elevator=true` (winda istnieje w budynku). Dodatkowo w trybie zero-shot `gemma3:4b` uznała, że skoro winda nie działa, to nie da się ustalić dostawy (`floor=null`).
4. **`NOTE-003` (`"Parter, wejście od podwórza"`), `NOTE-004` (`"IV piętro, lokal 12"`), `NOTE-009` (`"Drugie lub trzecie piętro"`)**  
   W wariancie `zero-shot` modele zamiast `elevator=null` wpisywały `false` lub `true` (dopowiadały brakujące dane). Problem ten został niemal całkowicie wyeliminowany w `gemma3:4b` po przejściu na `one-shot`. Ponadto w `NOTE-009` model `llama3.2:latest` mimo niejednoznaczności („Drugie lub trzecie piętro”) wybierał pierwszą liczbę (`floor=2` zamiast `null`).

---

## Rekomendacja

Który model i prompt wybieramy do projektu — i jakie mamy dowody, że to dobry wybór?

**Rekomendowana konfiguracja:**
- **Model:** `gemma3:4b`
- **Prompting:** `one-shot` (z przykładem pokazującym użycie wartości `null`)
- **Temperatura:** `0.0`
- **Format wyjścia:** natywny `json-schema` (`Extraction.Schema`) + walidacja i oczyszczanie odpowiedzi po stronie C# (`Extraction.Parse`).

**Dowody z przeprowadzonych eksperymentów:**
1. **Najwyższa skuteczność (`Exact match = 80%`):** W `EXP-02` połączenie `gemma3:4b` z promptem `one-shot` osiągnęło **90% Floor accuracy**, **90% Elevator accuracy** oraz **80% Exact match** — znacznie wyżej niż `llama3.2:latest` w jakimkolwiek wariancie (maksymalnie `50%` Exact match).
2. **Stabilność i determinizm:** W `EXP-03` potwierdzono, że `temperature = 0.0` gwarantuje 100% powtarzalności odpowiedzi przy wielokrotnych uruchomieniach, podczas gdy wyższe temperatury pogarszają stabilność (a w `llama3.2:latest` prowadzą nawet do błędów składni JSON).
3. **Niezawodność integracji (`10/10` Valid JSON):** W `EXP-04` tryb `json-schema` wyeliminował problem otaczania odpowiedzi znacznikami Markdown ```` ```json ```` przez `gemma3:4b` oraz skrócił średni czas odpowiedzi z ~713 ms do ~624 ms, co na MacBooku Air M5 (16 GB RAM) zapewnia w pełni akceptowalną szybkość działania przy wysokiej dokładności.
