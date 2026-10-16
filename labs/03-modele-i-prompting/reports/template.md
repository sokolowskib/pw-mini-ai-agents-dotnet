# Raport — Laboratorium 03

**Zespół:**  
**Data:**  

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
| gemma3:4b | 0 | 0 | 0| 691 |
| qwen3:4b| 100 | 100 | 100| 12244 |



**Wnioski:** Model qwen3:4b spisuje się świetnie wobec metryki exact_match odgadując wszystkie poprawnie, a gemma3:4b nie odgadł poprawnie ani razu.

## EXP-02 — Prompting

| Wariant | Floor accuracy | Elevator accuracy | Exact match |
|---------|----------------|-------------------|-------------|
| zero-shot | | | |
| one-shot | | | |
| few-shot | | | |

**Wnioski:**

## EXP-03 — Temperature

| Temperature | Exact match | Powtarzalne notatki |
|-------------|-------------|---------------------|
| 0.0 | | |
| 0.5 | | |
| 1.0 | | |

**Wnioski:**

## EXP-04 — Structured output — JSON i walidacja

| Wariant | Poprawny JSON | Exact match |
|---------|---------------|-------------|
| prompt | | |
| json-schema | | |

**Wnioski:**

## Najtrudniejsze notatki

Które notatki model najczęściej interpretował błędnie i dlaczego?

## Rekomendacja

Który model i prompt wybieramy do projektu — i jakie mamy dowody, że to dobry wybór?
