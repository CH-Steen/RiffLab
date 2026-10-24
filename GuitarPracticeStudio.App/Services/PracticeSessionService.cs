using System.IO;
using System.Text.Json;
using GuitarPracticeStudio.App.Models;

namespace GuitarPracticeStudio.App.Services
{
    public class PracticeSessionService
    {
        private readonly string _filePath;

        public static PracticeSessionService Instance { get; } = new();

        public List<PracticeSession> Sessions { get; private set; } = new();

        private PracticeSessionService()
        {
            _filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GuitarPracticeStudio",
                "practice-sessions.json");

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            LoadSessions();
        }

        public void LoadSessions()
        {
            if (!File.Exists(_filePath))
            {
                Sessions = new List<PracticeSession>();
                return;
            }

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Sessions = new List<PracticeSession>();
                return;
            }

            try
            {
                var loadedSessions = JsonSerializer.Deserialize<List<PracticeSession>>(json);
                Sessions = loadedSessions ?? new List<PracticeSession>();
            }
            catch
            {
                Sessions = new List<PracticeSession>();
            }
        }

        public bool TryAddSession(PracticeSession session)
        {
            if (session == null)
            {
                return false;
            }

            var isDuplicate = Sessions.Any(existing =>
                existing.Date == session.Date &&
                existing.Exercise == session.Exercise &&
                existing.DurationSeconds == session.DurationSeconds &&
                existing.GoalMinutes == session.GoalMinutes);

            if (isDuplicate)
            {
                return false;
            }

            Sessions.Add(session);
            SaveSessions();
            return true;
        }

        public bool DeleteSession(PracticeSession session)
        {
            if (session == null)
            {
                return false;
            }

            var removed = Sessions.Remove(session);
            if (removed)
            {
                SaveSessions();
            }

            return removed;
        }

        public IEnumerable<PracticeSession> GetRecentSessions(int count = 5)
        {
            return Sessions
                .OrderByDescending(session => session.Date)
                .Take(count)
                .ToList();
        }

        public TimeSpan GetTotalPracticeTime()
        {
            var totalSeconds = Sessions.Sum(session => session.DurationSeconds);
            return TimeSpan.FromSeconds(totalSeconds);
        }

        public int GetTotalSessions()
        {
            return Sessions.Count;
        }

        public int GetCurrentStreak()
        {
            if (Sessions.Count == 0)
            {
                return 0;
            }

            var uniqueDates = Sessions
                .Select(session => session.Date.Date)
                .Distinct()
                .OrderByDescending(date => date)
                .ToList();

            if (uniqueDates.Count == 0)
            {
                return 0;
            }

            var latestDate = uniqueDates[0];
            var currentDate = DateTime.Today;

            if (latestDate != currentDate && latestDate != currentDate.AddDays(-1))
            {
                return 0;
            }

            var streak = 0;
            var cursor = latestDate;

            while (uniqueDates.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }

            return streak;
        }

        public int GetTotalXp()
        {
            return Sessions.Sum(session => Math.Max(10, (session.DurationSeconds / 60) * 10));
        }

        public TimeSpan GetAveragePracticeTimePerActiveDay()
        {
            var activeDays = Sessions
                .Select(session => session.Date.Date)
                .Distinct()
                .Count();

            if (activeDays == 0)
            {
                return TimeSpan.Zero;
            }

            var totalSeconds = Sessions.Sum(session => session.DurationSeconds);
            return TimeSpan.FromSeconds(totalSeconds / activeDays);
        }

        public int GetLongestStreak()
        {
            if (Sessions.Count == 0)
            {
                return 0;
            }

            var dates = Sessions
                .Select(session => session.Date.Date)
                .Distinct()
                .OrderBy(date => date)
                .ToList();

            if (dates.Count == 0)
            {
                return 0;
            }

            var longest = 1;
            var current = 1;

            for (var i = 1; i < dates.Count; i++)
            {
                if (dates[i] == dates[i - 1].AddDays(1))
                {
                    current++;
                    longest = Math.Max(longest, current);
                }
                else
                {
                    current = 1;
                }
            }

            return longest;
        }

        public List<(string Exercise, int TotalSeconds)> GetMostPracticedExercises()
        {
            return Sessions
                .GroupBy(session => session.Exercise)
                .Select(group => (Exercise: group.Key, TotalSeconds: group.Sum(session => session.DurationSeconds)))
                .OrderByDescending(group => group.TotalSeconds)
                .ToList();
        }

        public TimeSpan GetLongestSession()
        {
            if (Sessions.Count == 0)
            {
                return TimeSpan.Zero;
            }

            var longestSeconds = Sessions.Max(session => session.DurationSeconds);
            return TimeSpan.FromSeconds(longestSeconds);
        }

        public List<(DateTime Date, int TotalSeconds)> GetLastSevenDaysActivity()
        {
            var list = new List<(DateTime Date, int TotalSeconds)>();
            for (var i = 6; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);
                var totalSeconds = Sessions
                    .Where(session => session.Date.Date == day)
                    .Sum(session => session.DurationSeconds);

                list.Add((day, totalSeconds));
            }

            return list;
        }

        public void SaveSessions()
        {
            var json = JsonSerializer.Serialize(Sessions, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_filePath, json);
        }
    }
}
