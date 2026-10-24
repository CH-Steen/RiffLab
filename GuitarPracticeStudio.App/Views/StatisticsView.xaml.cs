using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GuitarPracticeStudio.App.Models;
using GuitarPracticeStudio.App.Services;

namespace GuitarPracticeStudio.App.Views
{
    public partial class StatisticsView : UserControl
    {
        public StatisticsView()
        {
            InitializeComponent();
            Loaded += StatisticsView_Loaded;
        }

        private void StatisticsView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshStatistics();
        }

        public void RefreshStatistics()
        {
            var totalTime = PracticeSessionService.Instance.GetTotalPracticeTime();
            var totalSessions = PracticeSessionService.Instance.GetTotalSessions();
            var averageTime = PracticeSessionService.Instance.GetAveragePracticeTimePerActiveDay();
            var currentStreak = PracticeSessionService.Instance.GetCurrentStreak();
            var longestStreak = PracticeSessionService.Instance.GetLongestStreak();
            var totalXp = PracticeSessionService.Instance.GetTotalXp();
            var longestSession = PracticeSessionService.Instance.GetLongestSession();
            var mostPracticed = PracticeSessionService.Instance.GetMostPracticedExercises();
            var activity = PracticeSessionService.Instance.GetLastSevenDaysActivity();

            TotalPracticeTimeText.Text = FormatDuration(totalTime);
            TotalSessionsText.Text = totalSessions.ToString();
            AverageDailyTimeText.Text = FormatMinutes((int)Math.Round(averageTime.TotalMinutes));
            CurrentStreakText.Text = $"{currentStreak} days";
            LongestStreakText.Text = $"{longestStreak} days";
            TotalXpText.Text = totalXp.ToString();
            LongestSessionText.Text = FormatMinutes((int)Math.Round(longestSession.TotalMinutes));
            MostPracticedText.Text = mostPracticed.Count > 0
                ? $"{mostPracticed[0].Exercise} ({FormatMinutes((int)Math.Round(mostPracticed[0].TotalSeconds / 60.0))})"
                : "—";

            BuildActivityChart(activity);
            RefreshPracticeHistory();
        }

        private void DeletePracticeSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not PracticeSession session)
            {
                return;
            }

            var exerciseName = string.IsNullOrWhiteSpace(session.Exercise) ? "General Practice" : session.Exercise;
            var result = MessageBox.Show($"Delete the session for '{exerciseName}'?", "Delete session", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            PracticeSessionService.Instance.DeleteSession(session);
            RefreshStatistics();

            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.RefreshAllPageData();
        }

        private void RefreshPracticeHistory()
        {
            var sessions = PracticeSessionService.Instance.Sessions
                .OrderByDescending(session => session.Date)
                .Select(session => new PracticeHistoryEntry
                {
                    Session = session,
                    DisplayDate = session.Date.ToString("MMM d, yyyy"),
                    Exercise = (session.IsManual ? "Manual • " : string.Empty) + (string.IsNullOrWhiteSpace(session.Exercise) ? "General Practice" : session.Exercise),
                    DurationDisplay = FormatSessionDuration(session.DurationSeconds),
                    BpmDisplay = session.Bpm.HasValue ? $"BPM: {session.Bpm.Value}" : "BPM: n/a"
                })
                .ToList();

            PracticeHistoryListBox.ItemsSource = sessions;
            PracticeHistoryListBox.Visibility = sessions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            PracticeHistoryEmptyState.Visibility = sessions.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BuildActivityChart(List<(DateTime Date, int TotalSeconds)> activity)
        {
            ActivityChartGrid.Children.Clear();
            ActivityChartGrid.ColumnDefinitions.Clear();
            ActivityChartGrid.RowDefinitions.Clear();

            for (var i = 0; i < 7; i++)
            {
                ActivityChartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            ActivityChartGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            ActivityChartGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var maxValue = activity.Count == 0 ? 1 : activity.Max(day => day.TotalSeconds);

            for (var i = 0; i < activity.Count; i++)
            {
                var entry = activity[i];
                var value = maxValue == 0 ? 0 : entry.TotalSeconds / (double)maxValue;
                var bar = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(70, 229, 255)),
                    CornerRadius = new CornerRadius(8, 8, 0, 0),
                    Height = 150 * value,
                    Margin = new Thickness(4, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                Grid.SetColumn(bar, i);
                Grid.SetRow(bar, 0);
                ActivityChartGrid.Children.Add(bar);

                var label = new TextBlock
                {
                    Text = entry.Date.ToString("ddd"),
                    Foreground = new SolidColorBrush(Color.FromRgb(182, 198, 221)),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                Grid.SetColumn(label, i);
                Grid.SetRow(label, 1);
                ActivityChartGrid.Children.Add(label);
            }
        }

        private static string FormatDuration(TimeSpan time)
        {
            var hours = (int)time.TotalHours;
            var minutes = time.Minutes;
            return hours > 0 ? $"{hours}h {minutes:00}m" : $"{minutes}m";
        }

        private static string FormatSessionDuration(int durationSeconds)
        {
            var time = TimeSpan.FromSeconds(durationSeconds);
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }

        private static string FormatMinutes(int minutes)
        {
            if (minutes <= 0)
            {
                return "0m";
            }

            if (minutes < 60)
            {
                return $"{minutes}m";
            }

            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;
            return $"{hours}h {remainingMinutes:00}m";
        }

        private class PracticeHistoryEntry
        {
            public PracticeSession Session { get; set; } = new();
            public string DisplayDate { get; set; } = string.Empty;
            public string Exercise { get; set; } = string.Empty;
            public string DurationDisplay { get; set; } = string.Empty;
            public string BpmDisplay { get; set; } = string.Empty;
        }
    }
}
