namespace RiffLab.App.Models
{
    public class PracticeSession
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string Exercise { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public int? GoalMinutes { get; set; }
        public int? Bpm { get; set; }
        public bool IsManual { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
