public sealed class Metrics
{
    public int Total { get; private set; }
    public int ValidJson { get; private set; }
    public int WrappedInCodeBlock { get; private set; }
    public int FloorCorrect { get; private set; }
    public int ElevatorCorrect { get; private set; }
    public int ExactMatch { get; private set; }
    public long TotalMs { get; private set; }

    // Odsetek poprawnie rozpoznanych pięter
    public double FloorAccuracy => Ratio(FloorCorrect);

    // Odsetek poprawnie rozpoznanych informacji o windzie
    public double ElevatorAccuracy => Ratio(ElevatorCorrect);

    // Odsetek notatek, dla których oba pola są poprawne
    public double ExactMatchRate => Ratio(ExactMatch);

    public double AvgMs => Total == 0 ? 0 : (double)TotalMs / Total;

    public void Add(LabeledNote expected, ExtractionResult? result, long elapsedMs, bool wrapped = false)
    {
        Total++;
        TotalMs += elapsedMs;

        // Odpowiedź w bloku markdown parsujemy, ale liczymy jako niezgodność z instrukcją formatu
        if (wrapped)
            WrappedInCodeBlock++;

        // Niepoprawny JSON liczymy jako błędną odpowiedź
        if (result is null)
            return;

        ValidJson++;

        if (result.Floor == expected.Floor)
            FloorCorrect++;

        if (result.Elevator == expected.Elevator)
            ElevatorCorrect++;

        if (IsExactMatch(expected, result))
            ExactMatch++;
    }

    public static bool IsExactMatch(LabeledNote expected, ExtractionResult result) =>
        result.Floor == expected.Floor && result.Elevator == expected.Elevator;

    public override string ToString() =>
        $"Floor accuracy: {FloorAccuracy:P0}, " +
        $"Elevator accuracy: {ElevatorAccuracy:P0}, " +
        $"Exact match: {ExactMatchRate:P0}, " +
        $"poprawny JSON: {ValidJson}/{Total}, " +
        $"w bloku markdown: {WrappedInCodeBlock}/{Total}, " +
        $"średni czas: {AvgMs:F0} ms";

    private double Ratio(int value) => Total == 0 ? 0 : (double)value / Total;
}
