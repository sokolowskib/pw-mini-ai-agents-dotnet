# Raport — Laboratorium 03

**Zespół:**  Bartłomiej Sokołowski

**Data:**  22.10.2026

## Środowisko

| Parametr | Wartość |
|----------|---------|
| Wersja Ollama | 0.40.5 |
| Sprzęt (CPU/GPU, RAM) | Apple M4 Pro / 24GB|
| Liczba dostępnych notatek | 170 |

## EXP-00 — Sposób oceny wyników

Jak oceniamy poprawność odpowiedzi modelu? Jakie metryki przyjmujemy i dlaczego?

| Metryka | Definicja |
|---------|-----------|
| Exact match | odsetek notatek, dla których model poprawnie rozpoznał jednocześnie oba pola: `floor` i `elevator`|

## EXP-01 — Porównanie modeli

| Model | Floor accuracy | Elevator accuracy | Exact match | Średni czas [ms] |
|-------|----------------|-------------------|-------------|------------------|
| gemma3:4b | 78 | 46 | 42| 691 |
| qwen3:4b| 100 | 100 | 100| 12244 |



**Wnioski:** Model qwen3:4b spisuje się świetnie wobec metryki exact_match odgadując wszystkie poprawnie, a gemma3:4b nie odgadł poprawnie ani razu.

## EXP-02 — Prompting

**Model:** `gemma3:4b`

| Wariant | Floor accuracy | Elevator accuracy | Exact match |
|---------|----------------|-------------------|-------------|
| zero-shot | 78 | 46 | 42 |
| one-shot | 90 | 76 | 68 |
| few-shot | 72 | 86 | 70 |

**Wnioski:** Wykorzystaliśmy 50 przykładów. Podanie jednego przykładu poprawnej odpowiedzi znacznie zwiększa odsetek poprawnych odpowiedzi, natomiast podanie kilku przykładów wpłynęło nieznacznie.

## EXP-03 — Temperature

| Temperature | Exact match | Powtarzalne notatki |
|-------------|-------------|---------------------|
| 0.0 | 42 | 50/50 |
| 0.5 | 43 | 46/50|
| 1.0 | 43| 42/50|

**Wnioski:** Wraz z wzrostem temperatury determinizm modelu spada.

## EXP-04 — Structured output — JSON i walidacja

| Wariant | Poprawny JSON | Exact match |
|---------|---------------|-------------|
| prompt | 47/50 | 42 |
| json-schema |50/50 | 56|

**Wnioski:** Po podaniu json-schema, model ani razu nie zwrócił odpowiedzi w bloku markdown ```json ```. Wymuszenie struktury wyniku wpływa więc pozytywnie na poprawność modelu.

## Najtrudniejsze notatki

Które notatki model najczęściej interpretował błędnie i dlaczego?

Ranking na podstawie wszystkich przebiegów EXP-02, EXP-03 i EXP-04 (20 odpowiedzi na notatkę), szczegóły w `results/hardest-notes.csv`.

- **Kilka pięter w jednej notatce** (NOTE-003, NOTE-008, NOTE-035) — błędne w 95–100% przebiegów.
  - Oczekiwane jest `floor=null`, a model wybiera jedno z pięter.
  - Bez JSON Schema model potrafi zwrócić tablicę (`"floor": [3, 2, 1, -1]`), co kończy się błędem walidacji.
- **Piętro zapisane rzymską cyfrą ze skrótem `p.`** (NOTE-013, NOTE-014).
  - „P. I” model zwraca jako `floor=null` zamiast 1.
  - „p. IX” rozpoznaje poprawnie jako 9, ale dopowiada windę.
- **Dopowiadanie windy** (NOTE-005, NOTE-016, NOTE-044) — najczęstszy błąd ogółem.
  - Notatka nie wspomina o windzie, a model zwraca `elevator=false` zamiast `null`.
  - W wariancie zero-shot to 24 z 29 błędnych odpowiedzi.
- **Notatka bez piętra** (NOTE-020, „Komis samochodowy... biuro”).
  - Model zgaduje parter (`floor=0`) zamiast zwrócić `null`.

W zbiorze testowym jest 50 notatek, ale tylko 29 unikalnych treści, więc część trudnych przypadków jest liczona podwójnie.

## Rekomendacja

Który model i prompt wybieramy do projektu — i jakie mamy dowody, że to dobry wybór?

Rekomendujemy **`gemma3:4b`** z promptem **few-shot**, natywnym **JSON Schema** i **temperaturą 0**.

- **Czas odpowiedzi** — gemma odpowiada średnio w ~0,6 s, qwen3:4b w ~12 s, czyli ok. 20 razy wolniej. Przy 170 notatkach to ~2 minuty wobec ~35 minut przetwarzania, a różnica rośnie liniowo z liczbą notatek.
- **Prompt few-shot** — dał najwyższy exact match (70% wobec 42% dla zero-shot) i najlepiej rozpoznaje informację o windzie (86%), bo przykłady uczą model zostawiać `null`, gdy notatka nic o windzie nie mówi.
- **JSON Schema** — eliminuje błędy formatu (50/50 poprawnych odpowiedzi, 0 bloków markdown). Rozwiązuje też główny problem few-shot, czyli 6/50 niepoprawnych odpowiedzi, gdy model przepisywał format przykładów zamiast zwrócić JSON.
- **Temperatura 0** — wyższa temperatura nie poprawiła exact match (42–43%), a obniżyła powtarzalność odpowiedzi z 50/50 do 40/50 notatek. W ekstrakcji danych chcemy, żeby ta sama notatka zawsze dawała ten sam wynik.

**Ograniczenia:**

- Kombinacji few-shot + JSON Schema nie testowaliśmy w jednym przebiegu — rekomendacja łączy wyniki EXP-02 i EXP-04. Należy to potwierdzić osobnym pomiarem.
- qwen3:4b osiągnął 100% exact match, ale tylko na pierwszych 10 prostych notatkach. Jeśli na pełnym zbiorze 50 notatek utrzyma wyraźną przewagę, warto go rozważyć tam, gdzie czas odpowiedzi nie ma znaczenia, np. przy przetwarzaniu notatek wsadowo, z wyprzedzeniem.
- Nawet najlepszy wariant myli się w ok. 30% notatek, głównie gdy notatka wymienia kilka pięter. Wynik modelu powinien być traktowany jako podpowiedź dla kierowcy, a nie pewna informacja.
