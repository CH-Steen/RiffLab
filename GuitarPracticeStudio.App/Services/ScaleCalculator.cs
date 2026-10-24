namespace GuitarPracticeStudio.App.Services
{
    public static class ScaleCalculator
    {
        private static readonly string[] ChromaticNotes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        private static readonly int[] StandardTuningSemitones = { 4, 11, 7, 2, 9, 4 };

        private static readonly Dictionary<string, int[]> ScaleIntervals = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Major (Ionian)"] = new[] { 0, 2, 4, 5, 7, 9, 11 },
            ["Natural Minor (Aeolian)"] = new[] { 0, 2, 3, 5, 7, 8, 10 },
            ["Dorian"] = new[] { 0, 2, 3, 5, 7, 9, 10 },
            ["Phrygian"] = new[] { 0, 1, 3, 5, 7, 8, 10 },
            ["Lydian"] = new[] { 0, 2, 4, 6, 7, 9, 11 },
            ["Mixolydian"] = new[] { 0, 2, 4, 5, 7, 9, 10 },
            ["Locrian"] = new[] { 0, 1, 3, 5, 6, 8, 10 },
            ["Major Pentatonic"] = new[] { 0, 2, 4, 7, 9 },
            ["Minor Pentatonic"] = new[] { 0, 3, 5, 7, 10 },
            ["Blues"] = new[] { 0, 3, 5, 6, 7, 10 }
        };

        public static IReadOnlyList<string> RootNotes => ChromaticNotes;

        public static IReadOnlyList<string> ScaleNames => ScaleIntervals.Keys.ToList();

        public static IReadOnlyList<string> GetScaleNotes(string rootNote, string scaleName)
        {
            var rootIndex = GetNoteIndex(rootNote);
            var intervals = ScaleIntervals.TryGetValue(scaleName, out var scaleIntervals)
                ? scaleIntervals
                : ScaleIntervals["Major (Ionian)"];

            var notes = new List<string>();
            foreach (var interval in intervals)
            {
                var pitch = (rootIndex + interval) % 12;
                notes.Add(ChromaticNotes[pitch]);
            }

            return notes;
        }

        public static IReadOnlyList<(int StringIndex, int Fret, string NoteName, bool IsInScale, bool IsRoot)> GetFretboardDisplay(string rootNote, string scaleName)
        {
            var rootIndex = GetNoteIndex(rootNote);
            var scaleNotes = new HashSet<int>(GetScaleNotes(rootNote, scaleName).Select(note => GetNoteIndex(note)));
            var results = new List<(int StringIndex, int Fret, string NoteName, bool IsInScale, bool IsRoot)>();

            for (var stringIndex = 0; stringIndex < StandardTuningSemitones.Length; stringIndex++)
            {
                for (var fret = 0; fret <= 12; fret++)
                {
                    var pitch = (StandardTuningSemitones[stringIndex] + fret) % 12;
                    var noteName = ChromaticNotes[pitch];
                    var isRoot = pitch == rootIndex;
                    var isInScale = scaleNotes.Contains(pitch);

                    if (isInScale || isRoot)
                    {
                        results.Add((stringIndex, fret, noteName, true, isRoot));
                    }
                }
            }

            return results;
        }

        public static string BuildScaleSummary(string rootNote, string scaleName)
        {
            var notes = GetScaleNotes(rootNote, scaleName);
            return $"{rootNote} {scaleName}: {string.Join(" - ", notes)}";
        }

        private static int GetNoteIndex(string note)
        {
            for (var i = 0; i < ChromaticNotes.Length; i++)
            {
                if (string.Equals(ChromaticNotes[i], note, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(note), $"Unsupported note: {note}");
        }
    }
}
