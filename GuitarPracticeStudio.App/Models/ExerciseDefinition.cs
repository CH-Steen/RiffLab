namespace RiffLab.App.Models
{
    public class ExerciseDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Warmups";
        public string Difficulty { get; set; } = "Beginner";
        public string Description { get; set; } = string.Empty;
        public bool IsCustom { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
