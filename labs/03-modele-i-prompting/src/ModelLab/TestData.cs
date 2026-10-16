using System.Text;

public record LabeledNote(string Id, string Note, int? Floor, bool? Elevator);

public static class TestData
{
    // Notatki testowe z oczekiwanym wynikiem
    public static IReadOnlyList<LabeledNote> Examples => Load("labeled-notes.csv");

    // Przykłady używane w promptach one-shot i few-shot (inne niż notatki testowe)
    public static IReadOnlyList<LabeledNote> PromptExamples => Load("prompt-examples.csv");
    public static IReadOnlyList<LabeledNote> OneShowExample => Load("one-shot.csv");

    private static IReadOnlyList<LabeledNote> Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", fileName);

        return File.ReadLines(path)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseLine)
            // Pomijamy notatki, które nie mają jeszcze oczekiwanego wyniku
            .Where(fields => fields[2] != "" && fields[3] != "")
            .Select(fields => new LabeledNote(fields[0], fields[1], fields[2] == "null" ? null : int.Parse(fields[2]),
                                              fields[3] == "null" ? null : bool.Parse(fields[3])))
            .ToList();
    }

    // Prosty parser CSV: przecinek jako separator, pola w cudzysłowach mogą zawierać przecinki
    private static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                    quoted = false;
                else
                    field.Append(c);
            }
            else if (c == '"')
                quoted = true;
            else if (c == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
                field.Append(c);
        }

        fields.Add(field.ToString());

        return fields;
    }
}
