// EXP-01: porównanie dwóch modeli w ekstrakcji piętra i windy
public static class ModelComparison
{
    private static readonly string[] Models = ["gemma3:4b", "qwen3:4b"];

    public static async Task RunAsync(OllamaClient client)
    {
        foreach (var model in Models)
        {
            Console.WriteLine($"\nModel: {model}");

            var metrics =
                await Extraction.EvaluateAsync(client, model, TestData.Examples, note => Extraction.Prompt(note));

            ResultsCsv.Append("EXP-01", model, "zero-shot", 0, metrics);
        }
    }
}
