using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

public record ExtractionResult(int? Floor, bool? Elevator)
{
    public override string ToString() =>
        $"floor={Floor?.ToString() ?? "null"}, elevator={Elevator?.ToString().ToLower() ?? "null"}";
}

public static class Extraction
{
    // Schemat dla natywnego trybu structured output w Ollama
    public static readonly object Schema =
        new { type = "object",
              properties = new { floor = new { type = new[] { "integer", "null" } },
                                 elevator = new { type = new[] { "boolean", "null" } } },
              required = new[] { "floor", "elevator" } };

    // clang-format off
    public static string Prompt(string note, IEnumerable<LabeledNote>? examples = null)
    {
        var examplesSection = examples is null
            ? ""
            : $"""

                Przykłady:
                {string.Join(Environment.NewLine, examples.Select(e => $"\"{e.Note}\" => {ToJson(e.Floor, e.Elevator)}"))}

                """;

        return $$"""
            Z notatki kierowcy wyodrębnij dwie informacje:
            - floor — piętro docelowe dostawy (parter = 0); null, gdy nie można go jednoznacznie ustalić,
            - elevator — true, gdy notatka potwierdza obecność windy, false, gdy wyraźnie stwierdza jej brak, null, gdy brak informacji.

            Nie dopowiadaj brakujących danych.
            {{examplesSection}}
            Zwróć wyłącznie JSON:
            {
              "floor": liczba całkowita | null,
              "elevator": true | false | null
            }

            Zwróć czysty JSON, bez bloku markdown (```json ... ```).

            Notatka: "{{note}}"
            """;
    }
    // clang-format on

    // Sprawdzamy nie tylko deserializację, ale też wymagane pola i typy
    public static ExtractionResult? Parse(string response, out string? error)
    {
        try
        {
            using var json = JsonDocument.Parse(response);
            var root = json.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "To nie jest obiekt JSON";
                return null;
            }

            if (!root.TryGetProperty("floor", out var floor) ||
                !(floor.ValueKind == JsonValueKind.Null ||
                  floor.ValueKind == JsonValueKind.Number && floor.TryGetInt32(out _)))
            {
                error = "Brak pola floor lub zły typ";
                return null;
            }

            if (!root.TryGetProperty("elevator", out var elevator) ||
                elevator.ValueKind is not(JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null))
            {
                error = "Brak pola elevator lub zły typ";
                return null;
            }

            error = null;

            return new ExtractionResult(floor.ValueKind == JsonValueKind.Null ? null : floor.GetInt32(),
                                        elevator.ValueKind == JsonValueKind.Null ? null : elevator.GetBoolean());
        }
        catch (JsonException)
        {
            error = "Niepoprawny JSON";
            return null;
        }
    }

    // Uruchamia model dla wszystkich notatek i liczy metryki
    public static async Task<Metrics> EvaluateAsync(OllamaClient client, string model, IReadOnlyList<LabeledNote> notes,
                                                    Func<string, string> buildPrompt, double temperature = 0,
                                                    object? format = null)
    {
        var metrics = new Metrics();

        foreach (var note in notes)
        {
            var watch = Stopwatch.StartNew();

            var response = await client.GenerateAsync(model, buildPrompt(note.Note), temperature, format);

            watch.Stop();

            var wrapped = TryUnwrapCodeBlock(response, out var json);
            var result = Parse(json, out var error);

            metrics.Add(note, result, watch.ElapsedMilliseconds, wrapped);

            Console.WriteLine(
                result is null
                    ? $"✗ {note.Id} \"{note.Note}\" => {error}: {response}"
                    : $"{(Metrics.IsExactMatch(note, result) ? "✓" : "✗")} {note.Id} \"{note.Note}\" => {result} " +
                          $"(oczekiwano: {new ExtractionResult(note.Floor, note.Elevator)})");
        }

        Console.WriteLine(metrics);

        return metrics;
    }

    // Część modeli (np. gemma) mimo instrukcji opakowuje JSON w blok ```json ... ```
    public static bool TryUnwrapCodeBlock(string response, out string json)
    {
        var match = Regex.Match(response, @"^\s*```(?:json)?\s*(.*?)\s*```\s*$", RegexOptions.Singleline);

        json = match.Success ? match.Groups[1].Value : response;

        return match.Success;
    }

    private static string ToJson(int? floor, bool? elevator) => JsonSerializer.Serialize(new { floor, elevator });
}
