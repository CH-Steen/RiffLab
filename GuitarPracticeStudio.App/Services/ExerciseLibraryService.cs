using System.IO;
using System.Text.Json;
using RiffLab.App.Models;

namespace RiffLab.App.Services
{
    public class ExerciseLibraryService
    {
        private readonly string _customExercisePath;
        private readonly List<ExerciseDefinition> _customExercises = new();

        public static ExerciseLibraryService Instance { get; } = new();

        private ExerciseLibraryService()
        {
            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GuitarPracticeStudio");

            Directory.CreateDirectory(appDataFolder);
            _customExercisePath = Path.Combine(appDataFolder, "custom-exercises.json");
            LoadCustomExercises();
        }

        public List<ExerciseDefinition> GetAllExercises()
        {
            var exercises = new List<ExerciseDefinition>();
            exercises.AddRange(GetBuiltInExercises());
            exercises.AddRange(_customExercises);
            return exercises.OrderBy(e => e.Name).ToList();
        }

        public List<ExerciseDefinition> GetFilteredExercises(string? searchText, string? category, string? difficulty)
        {
            var query = GetAllExercises().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText.Trim();
                query = query.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(difficulty) && !difficulty.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.Difficulty.Equals(difficulty, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderBy(e => e.Name).ToList();
        }

        public List<string> GetCategories()
        {
            return new List<string>
            {
                "All",
                "Warmups",
                "Technique",
                "Chords",
                "Scales",
                "Rhythm",
                "Ear Training"
            };
        }

        public List<string> GetDifficulties()
        {
            return new List<string> { "All", "Beginner", "Intermediate", "Advanced" };
        }

        public bool AddCustomExercise(ExerciseDefinition exercise)
        {
            if (exercise == null)
            {
                return false;
            }

            exercise.IsCustom = true;
            exercise.Id = _customExercises.Count == 0 ? 1 : _customExercises.Max(e => e.Id) + 1;
            exercise.CreatedAt = DateTime.Now;
            _customExercises.Add(exercise);
            SaveCustomExercises();
            return true;
        }

        public bool UpdateCustomExercise(ExerciseDefinition exercise)
        {
            if (exercise == null)
            {
                return false;
            }

            var existing = _customExercises.FirstOrDefault(item => item.Id == exercise.Id);
            if (existing == null)
            {
                return false;
            }

            existing.Name = exercise.Name;
            existing.Category = exercise.Category;
            existing.Difficulty = exercise.Difficulty;
            existing.Description = exercise.Description;
            SaveCustomExercises();
            return true;
        }

        public bool DeleteCustomExercise(int id)
        {
            var match = _customExercises.FirstOrDefault(exercise => exercise.Id == id);
            if (match == null)
            {
                return false;
            }

            _customExercises.Remove(match);
            SaveCustomExercises();
            return true;
        }

        public ExerciseDefinition? GetExerciseByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return GetAllExercises().FirstOrDefault(exercise =>
                exercise.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadCustomExercises()
        {
            if (!File.Exists(_customExercisePath))
            {
                return;
            }

            var json = File.ReadAllText(_customExercisePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                var loaded = JsonSerializer.Deserialize<List<ExerciseDefinition>>(json);
                if (loaded != null)
                {
                    _customExercises.Clear();
                    _customExercises.AddRange(loaded);
                }
            }
            catch
            {
                _customExercises.Clear();
            }
        }

        private void SaveCustomExercises()
        {
            var json = JsonSerializer.Serialize(_customExercises, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_customExercisePath, json);
        }

        private static List<ExerciseDefinition> GetBuiltInExercises()
        {
            return new List<ExerciseDefinition>
            {
                new() { Id = 1000, Name = "Daily Warmup", Category = "Warmups", Difficulty = "Beginner", Description = "Loose fingers, easy rhythm, and clean picking control.", IsCustom = false },
                new() { Id = 1001, Name = "String Crossing Drill", Category = "Technique", Difficulty = "Beginner", Description = "Practice steady alternate picking across all strings.", IsCustom = false },
                new() { Id = 1002, Name = "Arpeggio Flow", Category = "Technique", Difficulty = "Intermediate", Description = "Smooth arpeggio patterns for precision and control.", IsCustom = false },
                new() { Id = 1003, Name = "Power Chord Shift", Category = "Chords", Difficulty = "Beginner", Description = "Move cleanly between open and bar chords with rhythm.", IsCustom = false },
                new() { Id = 1004, Name = "Major Scale Ladder", Category = "Scales", Difficulty = "Intermediate", Description = "Play a clean ascending and descending major scale pattern.", IsCustom = false },
                new() { Id = 1005, Name = "Minor Pentatonic Run", Category = "Scales", Difficulty = "Intermediate", Description = "Work on smooth bends and clean phrasing.", IsCustom = false },
                new() { Id = 1006, Name = "Metronome Pulse", Category = "Rhythm", Difficulty = "Beginner", Description = "Practice timing and downstroke consistency at a slow tempo.", IsCustom = false },
                new() { Id = 1007, Name = "Chord Strumming Builder", Category = "Rhythm", Difficulty = "Intermediate", Description = "Improve strumming pattern accuracy and groove.", IsCustom = false },
                new() { Id = 1008, Name = "Interval Recognition", Category = "Ear Training", Difficulty = "Advanced", Description = "Listen carefully and identify familiar intervals by ear.", IsCustom = false },
                new() { Id = 1009, Name = "Mode Awareness", Category = "Ear Training", Difficulty = "Advanced", Description = "Train your ear to hear modal color changes.", IsCustom = false }
            };
        }
    }
}
