using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GuitarPracticeStudio.App.Models;
using GuitarPracticeStudio.App.Services;

namespace GuitarPracticeStudio.App.Views
{
    public partial class CalendarView : UserControl
    {
        private DateTime _currentMonth = DateTime.Today;
        private DateTime _selectedDate = DateTime.Today;

        public CalendarView()
        {
            InitializeComponent();
            Loaded += CalendarView_Loaded;
            ManualExerciseComboBox.ItemsSource = ExerciseLibraryService.Instance.GetAllExercises()
                .Select(exercise => exercise.Name)
                .Append("General Practice")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            ManualExerciseComboBox.SelectedIndex = 0;
            ManualPracticeDatePicker.SelectedDate = _selectedDate;
            RefreshCalendar();
        }

        private void CalendarView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshCalendar();
        }

        public void RefreshCalendar()
        {
            MonthLabel.Text = _currentMonth.ToString("MMMM yyyy");
            CurrentStreakText.Text = $"Current streak: {PracticeSessionService.Instance.GetCurrentStreak()}";

            var calendarCells = new List<Button>();
            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            var startingDay = (int)firstDayOfMonth.DayOfWeek;
            var previousMonthDays = DateTime.DaysInMonth(firstDayOfMonth.AddMonths(-1).Year, firstDayOfMonth.AddMonths(-1).Month);

            CalendarGrid.Children.Clear();
            CalendarGrid.RowDefinitions.Clear();
            for (var i = 0; i < 6; i++)
            {
                CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            var sessionsByDate = PracticeSessionService.Instance.Sessions
                .GroupBy(session => session.Date.Date)
                .ToDictionary(group => group.Key, group => group.ToList());

            var dateCounter = 1;
            var nextMonthCounter = 1;

            for (var row = 0; row < 6; row++)
            {
                for (var col = 0; col < 7; col++)
                {
                    if (row == 0 && col < startingDay)
                    {
                        var date = firstDayOfMonth.AddDays(-(startingDay - col));
                        var button = CreateDayButton(date, sessionsByDate, isCurrentMonth: false);
                        Grid.SetColumn(button, col);
                        Grid.SetRow(button, row);
                        calendarCells.Add(button);
                        continue;
                    }

                    if (dateCounter > daysInMonth)
                    {
                        var date = new DateTime(_currentMonth.Year, _currentMonth.Month, 1).AddMonths(1).AddDays(nextMonthCounter - 1);
                        var button = CreateDayButton(date, sessionsByDate, isCurrentMonth: false);
                        Grid.SetColumn(button, col);
                        Grid.SetRow(button, row);
                        calendarCells.Add(button);
                        nextMonthCounter++;
                        continue;
                    }

                    var currentDate = new DateTime(_currentMonth.Year, _currentMonth.Month, dateCounter);
                    var currentButton = CreateDayButton(currentDate, sessionsByDate, isCurrentMonth: true);
                    Grid.SetColumn(currentButton, col);
                    Grid.SetRow(currentButton, row);
                    calendarCells.Add(currentButton);
                    dateCounter++;
                }
            }

            foreach (var dayButton in calendarCells)
            {
                CalendarGrid.Children.Add(dayButton);
            }

            UpdateSelectedDateDetails(_selectedDate);
        }

        private Button CreateDayButton(DateTime date, Dictionary<DateTime, List<PracticeSession>> sessionsByDate, bool isCurrentMonth)
        {
            var button = new Button
            {
                Margin = new Thickness(4),
                Padding = new Thickness(6),
                Height = 82,
                Background = isCurrentMonth ? new SolidColorBrush(Color.FromRgb(18, 26, 46)) : new SolidColorBrush(Color.FromRgb(14, 18, 32)),
                BorderBrush = date.Date == DateTime.Today ? new SolidColorBrush(Color.FromRgb(70, 229, 255)) : new SolidColorBrush(Color.FromRgb(43, 59, 93)),
                BorderThickness = date.Date == DateTime.Today ? new Thickness(1.5) : new Thickness(1),
                Tag = date,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stack = new StackPanel();
            var dayText = new TextBlock
            {
                Text = date.Day.ToString(),
                Foreground = isCurrentMonth ? Brushes.White : new SolidColorBrush(Color.FromRgb(120, 135, 170)),
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            stack.Children.Add(dayText);

            if (sessionsByDate.TryGetValue(date.Date, out var sessions))
            {
                var totalMinutes = (int)sessions.Sum(session => session.DurationSeconds) / 60;
                var summary = new TextBlock
                {
                    Text = totalMinutes > 0 ? $"{totalMinutes}m" : "Practice",
                    Foreground = new SolidColorBrush(Color.FromRgb(70, 229, 255)),
                    FontSize = 11,
                    Margin = new Thickness(0, 8, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                stack.Children.Add(summary);
            }

            button.Content = stack;
            button.Click += DayButton_Click;
            return button;
        }

        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DateTime date)
            {
                _selectedDate = date;
                UpdateSelectedDateDetails(date);
            }
        }

        private void UpdateSelectedDateDetails(DateTime date)
        {
            var sessions = PracticeSessionService.Instance.Sessions
                .Where(session => session.Date.Date == date.Date)
                .OrderByDescending(session => session.Date)
                .ToList();

            SelectedDateLabel.Text = date.ToString("ddd, MMM d, yyyy");

            if (sessions.Count == 0)
            {
                SelectedDateSessions.ItemsSource = new List<SessionSummaryItem>
                {
                    new() { Exercise = "No sessions", DurationText = "No practice recorded." }
                };
                return;
            }

            var summaryItems = sessions.Select(session => new SessionSummaryItem
            {
                Session = session,
                Exercise = (session.IsManual ? "Manual • " : string.Empty) + (string.IsNullOrWhiteSpace(session.Exercise) ? "General Practice" : session.Exercise),
                DurationText = FormatDuration(session.DurationSeconds)
            }).ToList();

            SelectedDateSessions.ItemsSource = summaryItems;
        }

        private void DeleteSelectedDateSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not PracticeSession session)
            {
                return;
            }

            var sessionName = string.IsNullOrWhiteSpace(session.Exercise) ? "General Practice" : session.Exercise;
            var result = MessageBox.Show($"Delete the session for '{sessionName}'?", "Delete session", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            PracticeSessionService.Instance.DeleteSession(session);
            RefreshCalendar();

            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.RefreshAllPageData();
        }

        private void AddPracticeButton_Click(object sender, RoutedEventArgs e)
        {
            ManualPracticeDatePicker.SelectedDate = _selectedDate;
            ManualPracticeMinutesTextBox.Text = "30";
            ManualExerciseComboBox.SelectedIndex = 0;
            ManualPracticeNotesTextBox.Text = string.Empty;
            ManualEntryPopup.IsOpen = true;
        }

        private void CancelManualEntryButton_Click(object sender, RoutedEventArgs e)
        {
            ManualEntryPopup.IsOpen = false;
        }

        private void SaveManualEntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ManualPracticeMinutesTextBox.Text, out var minutes) || minutes <= 0)
            {
                MessageBox.Show("Please enter a valid positive number of minutes.", "Invalid duration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedDate = ManualPracticeDatePicker.SelectedDate ?? DateTime.Today;
            var selectedExercise = ManualExerciseComboBox.SelectedItem?.ToString();
            var exerciseName = string.IsNullOrWhiteSpace(selectedExercise) ? "General Practice" : selectedExercise;
            var notes = ManualPracticeNotesTextBox.Text?.Trim() ?? string.Empty;

            var session = new PracticeSession
            {
                Date = selectedDate,
                Exercise = exerciseName,
                DurationSeconds = minutes * 60,
                GoalMinutes = minutes,
                IsManual = true,
                Notes = notes,
                Bpm = null
            };

            PracticeSessionService.Instance.TryAddSession(session);
            ManualEntryPopup.IsOpen = false;
            RefreshCalendar();

            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.RefreshAllPageData();
        }

        private static string FormatDuration(int totalSeconds)
        {
            var time = TimeSpan.FromSeconds(totalSeconds);
            return time.TotalHours >= 1
                ? $"{(int)time.TotalHours}h {time.Minutes:00}m {time.Seconds:00}s"
                : $"{time.Minutes}m {time.Seconds:00}s";
        }

        private void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            RefreshCalendar();
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            RefreshCalendar();
        }

        private sealed class SessionSummaryItem
        {
            public PracticeSession Session { get; set; } = new();
            public string Exercise { get; set; } = string.Empty;
            public string DurationText { get; set; } = string.Empty;
        }
    }
}
