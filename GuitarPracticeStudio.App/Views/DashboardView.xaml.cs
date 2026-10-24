using System.Windows;
using System.Windows.Controls;
using RiffLab.App.Services;

namespace RiffLab.App.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDashboard();
        }

        public void RefreshDashboard()
        {
            var sessions = PracticeSessionService.Instance.Sessions;
            var totalPracticeTime = PracticeSessionService.Instance.GetTotalPracticeTime();
            var totalSessions = PracticeSessionService.Instance.GetTotalSessions();
            var streak = PracticeSessionService.Instance.GetCurrentStreak();
            var totalXp = PracticeSessionService.Instance.GetTotalXp();
            var recentSessions = PracticeSessionService.Instance.GetRecentSessions(5).ToList();

            PracticeTimeValue.Text = FormatDuration(totalPracticeTime);
            PracticeTimeTrend.Text = totalSessions > 0 ? $"{totalSessions} completed session(s)" : "Ready to practice";
            CurrentStreakValue.Text = $"{streak} days";
            CurrentStreakTrend.Text = streak > 0 ? "Consistency is growing" : "Start a streak";
            TotalXpValue.Text = totalXp.ToString();
            LevelText.Text = $"Level {Math.Max(1, (totalXp / 250) + 1)}";
            SessionCountValue.Text = totalSessions.ToString();
            DashboardBadgeText.Text = $"{Math.Min(7, totalSessions)} practice days this week";

            RecentSessionsList.ItemsSource = recentSessions;
        }

        private static string FormatDuration(TimeSpan time)
        {
            var hours = (int)time.TotalHours;
            var minutes = time.Minutes;
            return $"{hours}h {minutes:00}m";
        }
    }
}
