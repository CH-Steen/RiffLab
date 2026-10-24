using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using GuitarPracticeStudio.App.Models;
using GuitarPracticeStudio.App.Services;

namespace GuitarPracticeStudio.App.Views
{
    public partial class PracticeView : UserControl
    {
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch = new();
        private readonly Queue<DateTime> _tapTimes = new();
        private readonly List<Ellipse> _beatIndicators = new();
        private bool _isSessionStarted;
        private bool _sessionSaved;
        private bool _isInitializing;

        public PracticeView()
        {
            _isInitializing = true;
            InitializeComponent();
            _isInitializing = false;
            InitializeExerciseList();
            GoalMinutesTextBox.ItemsSource = new[] { "5", "10", "15", "20", "30", "45", "60", "90", "120" };
            GoalMinutesTextBox.SelectedIndex = 4;
            GoalMinutesTextBox.Text = "30";
            GoalMinutesTextBox.SelectionChanged += GoalMinutesTextBox_SelectionChanged;
            GoalMinutesTextBox.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(GoalMinutesTextBox_TextChanged));

            UpdateCurrentExerciseDisplay();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += TimerTick;

            _beatIndicators.Add(BeatIndicator1);
            _beatIndicators.Add(BeatIndicator2);
            _beatIndicators.Add(BeatIndicator3);
            _beatIndicators.Add(BeatIndicator4);
            _beatIndicators.Add(BeatIndicator5);
            _beatIndicators.Add(BeatIndicator6);

            SharedMetronomeService.StateChanged += RefreshMetronomeUi;
            MetronomeBpmSlider.Minimum = 40;
            MetronomeBpmSlider.Maximum = 240;
            MetronomeBpmSlider.Value = SharedMetronomeService.Bpm;
            MetronomeBpmTextBox.Text = SharedMetronomeService.Bpm.ToString();
            MetronomeVolumeSlider.Value = SharedMetronomeService.Volume * 100;
            MetronomeVolumeTextBlock.Text = $"{(int)Math.Round(SharedMetronomeService.Volume * 100)}%";

            TimeSignatureComboBox.ItemsSource = new[] { "2/4", "3/4", "4/4", "6/8" };
            TimeSignatureComboBox.SelectedIndex = 2;
            TimeSignatureComboBox.SelectionChanged += TimeSignatureComboBox_SelectionChanged;

            RefreshMetronomeUi();
            UpdateDisplay();
            UpdateButtonStates();
        }

        public void SetSelectedExercise(string? exerciseName)
        {
            if (string.IsNullOrWhiteSpace(exerciseName))
            {
                return;
            }

            if (ExerciseComboBox.ItemsSource is IEnumerable<ExerciseDefinition> exercises)
            {
                var match = exercises.FirstOrDefault(exercise =>
                    string.Equals(exercise.Name, exerciseName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    ExerciseComboBox.SelectedItem = match;
                    return;
                }
            }

            var target = ExerciseLibraryService.Instance.GetExerciseByName(exerciseName);
            if (target != null)
            {
                ExerciseComboBox.SelectedItem = target;
            }
        }

        private void InitializeExerciseList()
        {
            ExerciseComboBox.ItemsSource = ExerciseLibraryService.Instance.GetAllExercises();
            ExerciseComboBox.DisplayMemberPath = nameof(ExerciseDefinition.Name);
            ExerciseComboBox.SelectedValuePath = nameof(ExerciseDefinition.Name);
            ExerciseComboBox.SelectionChanged += (_, _) => UpdateCurrentExerciseDisplay();
            ExerciseComboBox.SelectedIndex = 0;
        }

        private void UpdateCurrentExerciseDisplay()
        {
            var selected = ExerciseComboBox.SelectedItem as ExerciseDefinition;

            if (selected == null)
            {
                CurrentExerciseNameTextBlock.Text = "Select an exercise to get started.";
                CurrentExerciseDescriptionTextBlock.Text = "Choose a warmup, scale, or technique exercise from the list to begin your practice session.";
                return;
            }

            CurrentExerciseNameTextBlock.Text = selected.Name;
            CurrentExerciseDescriptionTextBlock.Text = string.IsNullOrWhiteSpace(selected.Description)
                ? "No description available for this exercise yet."
                : selected.Description;
        }

        private void StartSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSessionStarted)
            {
                return;
            }

            _sessionSaved = false;
            _isSessionStarted = true;
            _stopwatch.Restart();
            _timer.Start();
            UpdateDisplay();
            UpdateButtonStates();
        }

        private void PauseSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSessionStarted || !_stopwatch.IsRunning)
            {
                return;
            }

            _stopwatch.Stop();
            _timer.Stop();
            UpdateDisplay();
            UpdateButtonStates();
        }

        private void ResumeSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSessionStarted || _stopwatch.IsRunning)
            {
                return;
            }

            _stopwatch.Start();
            _timer.Start();
            UpdateDisplay();
            UpdateButtonStates();
        }

        private void StopSessionButton_Click(object sender, RoutedEventArgs e)
        {
            var elapsed = _stopwatch.Elapsed;
            if (!_isSessionStarted && elapsed == TimeSpan.Zero)
            {
                MessageBox.Show("There is no active session to stop.", "No session running", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var message = $"Stop the current session?\n\nExercise: {GetSelectedExercise()}\nTotal practice time: {FormatDuration(elapsed)}";
            if (MessageBox.Show(message, "Discard unfinished session?", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            _stopwatch.Stop();
            _timer.Stop();
            var finalElapsed = _stopwatch.Elapsed;

            if (!_sessionSaved && finalElapsed > TimeSpan.Zero)
            {
                var sessionToSave = new PracticeSession
                {
                    Date = DateTime.Now,
                    Exercise = GetSelectedExercise(),
                    DurationSeconds = (int)finalElapsed.TotalSeconds,
                    GoalMinutes = GetGoalMinutes(),
                    Bpm = SharedMetronomeService.Bpm
                };

                PracticeSessionService.Instance.TryAddSession(sessionToSave);
                _sessionSaved = true;
            }

            _stopwatch.Reset();
            _isSessionStarted = false;

            UpdateDisplay();
            UpdateButtonStates();

            MessageBox.Show(
                $"Session Summary\n\nExercise: {GetSelectedExercise()}\nTotal practice time: {FormatDuration(finalElapsed)}",
                "Practice session complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void TimerTick(object? sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void GoalMinutesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDisplay();
        }

        private void GoalMinutesTextBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GoalMinutesTextBox.SelectedItem is string selected)
            {
                GoalMinutesTextBox.Text = selected;
            }

            UpdateDisplay();
        }

        private void MetronomeStartButton_Click(object sender, RoutedEventArgs e)
        {
            SharedMetronomeService.Start();
            RefreshMetronomeUi();
        }

        private void MetronomeStopButton_Click(object sender, RoutedEventArgs e)
        {
            SharedMetronomeService.Stop();
            RefreshMetronomeUi();
        }

        private void MetronomeBpmSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || MetronomeBpmSlider == null || MetronomeBpmTextBox == null)
            {
                return;
            }

            var value = (int)Math.Round(MetronomeBpmSlider.Value);
            SharedMetronomeService.SetBpm(value);
            if (MetronomeBpmTextBox.Text != value.ToString())
            {
                MetronomeBpmTextBox.Text = value.ToString();
            }
        }

        private void MetronomeBpmTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || MetronomeBpmTextBox == null || MetronomeBpmSlider == null)
            {
                return;
            }

            if (!int.TryParse(MetronomeBpmTextBox.Text, out var value))
            {
                return;
            }

            value = Math.Clamp(value, 40, 240);
            SharedMetronomeService.SetBpm(value);
            if (MetronomeBpmSlider.Value != value)
            {
                MetronomeBpmSlider.Value = value;
            }
            if (MetronomeBpmTextBox.Text != value.ToString())
            {
                MetronomeBpmTextBox.Text = value.ToString();
            }
        }

        private void MetronomeVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || MetronomeVolumeSlider == null || MetronomeVolumeTextBlock == null)
            {
                return;
            }

            SharedMetronomeService.SetVolume(MetronomeVolumeSlider.Value / 100.0);
            MetronomeVolumeTextBlock.Text = $"{(int)Math.Round(MetronomeVolumeSlider.Value)}%";
        }

        private void TimeSignatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SharedMetronomeService.SetBeatsPerMeasure(GetBeatsPerMeasure());
            UpdateBeatIndicators();
        }

        private void TapTempoButton_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            _tapTimes.Enqueue(now);

            while (_tapTimes.Count > 5)
            {
                _tapTimes.Dequeue();
            }

            if (_tapTimes.Count < 2)
            {
                return;
            }

            var times = _tapTimes.ToArray();
            var intervals = new List<double>();
            for (var i = 1; i < times.Length; i++)
            {
                intervals.Add((times[i] - times[i - 1]).TotalMilliseconds);
            }

            var averageInterval = intervals.Average();
            if (averageInterval <= 0)
            {
                return;
            }

            var nextBpm = (int)Math.Round(60000.0 / averageInterval);
            nextBpm = Math.Clamp(nextBpm, 40, 240);
            SharedMetronomeService.SetBpm(nextBpm);
            MetronomeBpmTextBox.Text = nextBpm.ToString();
            MetronomeBpmSlider.Value = nextBpm;
        }

        private int GetBeatsPerMeasure()
        {
            return TimeSignatureComboBox.SelectedItem switch
            {
                "2/4" => 2,
                "3/4" => 3,
                "6/8" => 6,
                _ => 4
            };
        }

        private void RefreshMetronomeUi()
        {
            var value = SharedMetronomeService.Bpm;
            if (MetronomeBpmSlider.Value != value)
            {
                MetronomeBpmSlider.Value = value;
            }

            if (MetronomeBpmTextBox.Text != value.ToString())
            {
                MetronomeBpmTextBox.Text = value.ToString();
            }

            var volumePercent = (int)Math.Round(SharedMetronomeService.Volume * 100);
            if (MetronomeVolumeSlider.Value != volumePercent)
            {
                MetronomeVolumeSlider.Value = volumePercent;
            }
            MetronomeVolumeTextBlock.Text = $"{volumePercent}%";
            UpdateBeatIndicators();
        }

        private void UpdateBeatIndicators()
        {
            var beatCount = SharedMetronomeService.BeatsPerMeasure;

            for (var i = 0; i < _beatIndicators.Count; i++)
            {
                var ellipse = _beatIndicators[i];
                if (i >= beatCount)
                {
                    ellipse.Visibility = Visibility.Collapsed;
                    continue;
                }

                ellipse.Visibility = Visibility.Visible;
                ellipse.Fill = i == SharedMetronomeService.CurrentBeatIndex && SharedMetronomeService.IsRunning
                    ? new SolidColorBrush(Color.FromRgb(70, 229, 255))
                    : new SolidColorBrush(Color.FromRgb(43, 59, 93));
                ellipse.Stroke = new SolidColorBrush(Color.FromRgb(70, 229, 255));
                ellipse.Opacity = i == SharedMetronomeService.CurrentBeatIndex && SharedMetronomeService.IsRunning ? 1.0 : 0.7;
            }
        }

        private void UpdateDisplay()
        {
            var elapsed = _stopwatch.Elapsed;
            var goalMinutes = GetGoalMinutes();
            var progress = goalMinutes > 0 ? (elapsed.TotalMinutes / goalMinutes) * 100 : 0;
            var boundedProgress = Math.Min(progress, 100);

            TimerTextBlock.Text = FormatDuration(elapsed);
            SessionProgressBar.Value = boundedProgress;
            ProgressTextBlock.Text = $"{FormatDuration(elapsed)} / {FormatDuration(TimeSpan.FromMinutes(goalMinutes))}";
            StatusTextBlock.Text = _stopwatch.IsRunning ? "Running" : (_isSessionStarted ? "Paused" : "Idle");
            SessionSummaryTextBlock.Text = _isSessionStarted
                ? $"Working on {GetSelectedExercise()} with a {goalMinutes}-minute goal."
                : "Ready to begin a focused practice session.";
            UpdateCurrentExerciseDisplay();
        }

        private void UpdateButtonStates()
        {
            var hasActiveSession = _isSessionStarted || _stopwatch.Elapsed > TimeSpan.Zero;
            StartSessionButton.IsEnabled = !_isSessionStarted && !_stopwatch.IsRunning && _stopwatch.Elapsed == TimeSpan.Zero;
            PauseSessionButton.IsEnabled = _isSessionStarted && _stopwatch.IsRunning;
            ResumeSessionButton.IsEnabled = _isSessionStarted && !_stopwatch.IsRunning && _stopwatch.Elapsed > TimeSpan.Zero;
            StopSessionButton.IsEnabled = hasActiveSession;
        }

        private int GetGoalMinutes()
        {
            if (int.TryParse(GoalMinutesTextBox.Text, out var minutes) && minutes > 0)
            {
                return minutes;
            }

            return 30;
        }

        private string GetSelectedExercise()
        {
            return (ExerciseComboBox.SelectedItem as ExerciseDefinition)?.Name ?? "General Practice";
        }

        private static string FormatDuration(TimeSpan time)
        {
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}
