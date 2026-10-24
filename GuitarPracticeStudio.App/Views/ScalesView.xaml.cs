using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio.Wave.SampleProviders;
using GuitarPracticeStudio.App.Controls;
using GuitarPracticeStudio.App.Models;
using GuitarPracticeStudio.App.Services;
using Microsoft.Win32;

namespace GuitarPracticeStudio.App.Views
{
    public partial class ScalesView : UserControl
    {
        private readonly DispatcherTimer _backingTrackTimer = new();
        private readonly List<(double Min, double Max)> _waveformPeaks = new();
        private int _waveformRequestId;
        private bool _isUpdatingBackingTrackSlider;
        private bool _isScrubbingWaveform;
        private bool _isSelectingLoop;
        private double _loopSelectionStart;
        private double _loopSelectionEnd;
        private bool _loopEnabled;
        private int _trackBpm = 120;
        private int _trackBeatsPerMeasure = 4;

        public ScalesView()
        {
            InitializeComponent();
            Loaded += ScalesView_Loaded;
            SizeChanged += ScalesView_SizeChanged;
            PopulateSelectors();
            InitializeMetronomeControls();
            InitializeBackingTrackControls();
            RootNoteComboBox.SelectionChanged += (_, _) => UpdateScaleDisplay();
            ScaleComboBox.SelectionChanged += (_, _) => UpdateScaleDisplay();
            SharedMetronomeService.StateChanged += RefreshMetronomeControls;
            BackingTrackService.StateChanged += RefreshBackingTrackUi;

            _backingTrackTimer.Interval = TimeSpan.FromMilliseconds(200);
            _backingTrackTimer.Tick += (_, _) => RefreshBackingTrackUi();
            _backingTrackTimer.Start();

            UpdateScaleDisplay();
            RefreshMetronomeControls();
            RefreshBackingTrackUi();
            DrawTimelineRuler();
        }

        private void PopulateSelectors()
        {
            RootNoteComboBox.ItemsSource = ScaleCalculator.RootNotes;
            ScaleComboBox.ItemsSource = ScaleCalculator.ScaleNames;
            RootNoteComboBox.SelectedIndex = 9;
            ScaleComboBox.SelectedIndex = 0;
        }

        private void InitializeMetronomeControls()
        {
            ScalesMetronomeBpmSlider.Minimum = 40;
            ScalesMetronomeBpmSlider.Maximum = 240;
            ScalesMetronomeBpmSlider.Value = SharedMetronomeService.Bpm;
            ScalesMetronomeBpmTextBox.Text = SharedMetronomeService.Bpm.ToString();
        }

        private void InitializeBackingTrackControls()
        {
            _trackBeatsPerMeasure = 4;
            BackingTrackBpmTextBox.Text = _trackBpm.ToString();
        }

        private void RefreshMetronomeControls()
        {
            var bpm = SharedMetronomeService.Bpm;
            if (ScalesMetronomeBpmSlider.Value != bpm)
            {
                ScalesMetronomeBpmSlider.Value = bpm;
            }

            if (ScalesMetronomeBpmTextBox.Text != bpm.ToString())
            {
                ScalesMetronomeBpmTextBox.Text = bpm.ToString();
            }

            var volumePercent = (int)Math.Round(SharedMetronomeService.Volume * 100.0);
            if (ScalesMetronomeVolumeSlider.Value != volumePercent)
            {
                ScalesMetronomeVolumeSlider.Value = volumePercent;
            }

            ScalesMetronomePlayButton.Content = SharedMetronomeService.IsRunning ? "Pause" : "Play";
        }

        private void ScalesMetronomePlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedMetronomeService.IsRunning)
            {
                SharedMetronomeService.Stop();
            }
            else
            {
                SharedMetronomeService.Start();
            }

            RefreshMetronomeControls();
        }

        private void ScalesMetronomeBpmSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ScalesMetronomeBpmTextBox == null)
            {
                return;
            }

            var value = (int)Math.Round(ScalesMetronomeBpmSlider.Value);
            SharedMetronomeService.SetBpm(value);
            ScalesMetronomeBpmTextBox.Text = value.ToString();
        }

        private void ScalesMetronomeBpmTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(ScalesMetronomeBpmTextBox.Text, out var value))
            {
                return;
            }

            value = Math.Clamp(value, 40, 240);
            SharedMetronomeService.SetBpm(value);
            if (ScalesMetronomeBpmSlider.Value != value)
            {
                ScalesMetronomeBpmSlider.Value = value;
            }
        }

        private void ScalesMetronomeVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SharedMetronomeService.SetVolume(ScalesMetronomeVolumeSlider.Value / 100.0);
        }

        private void BackingTrackBpmTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BackingTrackBpmTextBox.Text, out var value))
            {
                return;
            }

            _trackBpm = Math.Clamp(value, 40, 240);
            BackingTrackBpmTextBox.Text = _trackBpm.ToString();
            DrawTimelineRuler();
        }

        private void OpenBackingTrackButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a backing track",
                Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            TryLoadBackingTrack(dialog.FileName);
        }

        private void BackingTrackToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleBackingTrackPlayback();
        }

        private void BackingTrackRestartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!BackingTrackService.IsLoaded)
            {
                return;
            }

            BackingTrackService.Seek(0d);
            RefreshBackingTrackUi();
        }

        private void BackingTrackRewindButton_Click(object sender, RoutedEventArgs e)
        {
            SeekBackingTrackRelative(-10d);
        }

        private void BackingTrackForwardButton_Click(object sender, RoutedEventArgs e)
        {
            SeekBackingTrackRelative(10d);
        }

        private void BackingTrackEndButton_Click(object sender, RoutedEventArgs e)
        {
            if (!BackingTrackService.IsLoaded)
            {
                return;
            }

            BackingTrackService.Seek(BackingTrackService.TotalSeconds);
            RefreshBackingTrackUi();
        }

        private void BackingTrackVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            BackingTrackService.SetVolume(BackingTrackVolumeSlider.Value / 100.0);
        }

        private void BackingTrackDropBorder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                BackingTrackDropBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 229, 255));
                BackingTrackDropBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 26, 44));
            }
        }

        private void BackingTrackDropBorder_DragLeave(object sender, DragEventArgs e)
        {
            BackingTrackDropBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 57, 95));
            BackingTrackDropBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 26, 44));
        }

        private void BackingTrackDropBorder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            {
                return;
            }

            var file = files.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
            if (string.IsNullOrWhiteSpace(file))
            {
                return;
            }

            TryLoadBackingTrack(file);
            BackingTrackDropBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 57, 95));
            BackingTrackDropBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 26, 44));
        }

        private void TryLoadBackingTrack(string path)
        {
            if (!BackingTrackService.Load(path))
            {
                LoadedBackingTrackTextBlock.Text = BackingTrackService.LastErrorMessage ?? "Unable to load this file.";
                _waveformPeaks.Clear();
                DrawWaveform(Array.Empty<(double Min, double Max)>());
                MessageBox.Show(
                    BackingTrackService.LastErrorMessage ?? "Unable to load this file.",
                    "Backing track load failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _ = GenerateWaveformAsync(path);
            RefreshBackingTrackUi();
        }

        private Task GenerateWaveformAsync(string path)
        {
            var requestId = Interlocked.Increment(ref _waveformRequestId);
            return Task.Run(() =>
            {
                try
                {
                    var peaks = BuildWaveformPeaks(path, 700);
                    if (requestId != _waveformRequestId)
                    {
                        return;
                    }

                    _waveformPeaks.Clear();
                    _waveformPeaks.AddRange(peaks);
                    Dispatcher.BeginInvoke(new Action(() => DrawWaveform(peaks)));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Backing track waveform generation failed for '{path}': {ex.GetType().Name}: {ex.Message}");
                }
            });
        }

        private static IReadOnlyList<(double Min, double Max)> BuildWaveformPeaks(string path, int targetWidth)
        {
            using var reader = new NAudio.Wave.AudioFileReader(path);
            var samples = new List<float>();
            var buffer = new byte[4096];
            var bitsPerSample = reader.WaveFormat.BitsPerSample;
            var isFloat = reader.WaveFormat.Encoding == NAudio.Wave.WaveFormatEncoding.IeeeFloat;
            var bytesPerSample = Math.Max(1, bitsPerSample / 8);

            while (true)
            {
                var read = reader.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                var sampleCount = read / bytesPerSample;
                for (var i = 0; i < sampleCount; i++)
                {
                    var offset = i * bytesPerSample;
                    float sample;
                    if (isFloat)
                    {
                        sample = BitConverter.ToSingle(buffer, offset);
                    }
                    else if (bitsPerSample == 8)
                    {
                        sample = (sbyte)buffer[offset] / 128f;
                    }
                    else if (bitsPerSample == 16)
                    {
                        sample = BitConverter.ToInt16(buffer, offset) / 32768f;
                    }
                    else if (bitsPerSample == 24)
                    {
                        var value = buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16);
                        if ((value & 0x00800000) != 0)
                        {
                            value |= unchecked((int)0xFF000000);
                        }

                        sample = value / 8388608f;
                    }
                    else
                    {
                        sample = BitConverter.ToInt32(buffer, offset) / 2147483648f;
                    }

                    samples.Add(sample);
                }
            }

            if (samples.Count == 0)
            {
                return Array.Empty<(double Min, double Max)>();
            }

            var peaks = new List<(double Min, double Max)>();
            var samplesPerBucket = Math.Max(1, samples.Count / Math.Max(1, targetWidth));
            for (var i = 0; i < targetWidth; i++)
            {
                var start = i * samplesPerBucket;
                var end = Math.Min(start + samplesPerBucket, samples.Count);
                if (end <= start)
                {
                    peaks.Add((0d, 0d));
                    continue;
                }

                double min = double.PositiveInfinity;
                double max = double.NegativeInfinity;
                for (var j = start; j < end; j++)
                {
                    var value = samples[j];
                    if (value < min)
                    {
                        min = value;
                    }

                    if (value > max)
                    {
                        max = value;
                    }
                }

                peaks.Add((min, max));
            }

            return peaks;
        }

        private void ScalesView_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => DrawTimelineRuler()), DispatcherPriority.Loaded);
        }

        private void ScalesView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth > 0 && ActualHeight > 0)
            {
                DrawTimelineRuler();
            }
        }

        private void BackingTrackTimelineCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawTimelineRuler();
            UpdateLoopSelectionVisual();
        }

        private void BackingTrackWaveformCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_waveformPeaks.Count > 0)
            {
                DrawWaveform(_waveformPeaks);
            }

            UpdateLoopSelectionVisual();
        }

        private void DrawTimelineRuler()
        {
            if (BackingTrackTimelineCanvas == null)
            {
                return;
            }

            BackingTrackTimelineCanvas.Children.Clear();
            var loopSelection = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(0xAA, 0x30, 0x38, 0xFF)),
                Stroke = new SolidColorBrush(Color.FromRgb(122, 247, 255)),
                StrokeThickness = 1,
                Visibility = Visibility.Collapsed
            };
            Canvas.SetTop(loopSelection, 0);
            BackingTrackTimelineCanvas.Children.Add(loopSelection);
            BackingTrackTimelineLoopSelection = loopSelection;

            var width = BackingTrackTimelineCanvas.ActualWidth;
            if (double.IsNaN(width) || width <= 0d)
            {
                return;
            }

            var duration = BackingTrackService.IsLoaded ? BackingTrackService.TotalSeconds : GetDefaultTimelineDuration();
            if (duration <= 0d)
            {
                return;
            }

            var beatsPerMeasure = 4;
            var beatDurationSeconds = 60d / Math.Max(1, _trackBpm);
            var measureDurationSeconds = beatDurationSeconds * beatsPerMeasure;
            var measureCount = Math.Max(1, (int)Math.Ceiling(duration / measureDurationSeconds));
            for (var measureIndex = 0; measureIndex <= measureCount; measureIndex++)
            {
                var measureStartSeconds = measureIndex * measureDurationSeconds;
                if (measureStartSeconds > duration)
                {
                    break;
                }

                var measureX = width * (measureStartSeconds / duration);
                var majorLine = new Line
                {
                    X1 = measureX,
                    Y1 = 0,
                    X2 = measureX,
                    Y2 = 16,
                    Stroke = new SolidColorBrush(Color.FromRgb(122, 247, 255)),
                    StrokeThickness = 1.25
                };
                BackingTrackTimelineCanvas.Children.Add(majorLine);

                var measureLabel = new TextBlock
                {
                    Text = $"{measureIndex + 1}",
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 230, 255)),
                    FontSize = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                var maxLabelX = Math.Max(0d, width - 18d);
                Canvas.SetLeft(measureLabel, Math.Clamp(measureX + 2d, 0d, maxLabelX));
                BackingTrackTimelineCanvas.Children.Add(measureLabel);

                for (var beatIndex = 1; beatIndex < beatsPerMeasure; beatIndex++)
                {
                    var beatSeconds = measureStartSeconds + beatIndex * beatDurationSeconds;
                    if (beatSeconds > duration)
                    {
                        break;
                    }

                    var beatX = width * (beatSeconds / duration);
                    var beatLine = new Line
                    {
                        X1 = beatX,
                        Y1 = 0,
                        X2 = beatX,
                        Y2 = 10,
                        Stroke = new SolidColorBrush(Color.FromRgb(115, 146, 189)),
                        StrokeThickness = 1
                    };
                    BackingTrackTimelineCanvas.Children.Add(beatLine);
                }
            }

            UpdateLoopSelectionVisual();
        }

        private double GetDefaultTimelineDuration()
        {
            var measureDuration = (60d / Math.Max(1, _trackBpm)) * 4;
            return measureDuration * 16d;
        }

        private void DrawWaveform(IReadOnlyList<(double Min, double Max)> peaks)
        {
            if (BackingTrackWaveformCanvas == null)
            {
                return;
            }

            BackingTrackWaveformCanvas.Children.Clear();
            var width = Math.Max(1, (int)BackingTrackWaveformCanvas.ActualWidth);
            var height = Math.Max(1, (int)BackingTrackWaveformCanvas.ActualHeight);
            var halfHeight = height / 2d;
            var amplitude = Math.Max(0.15d, Math.Min(0.9d, halfHeight / 35d));

            if (peaks.Count == 0)
            {
                BackingTrackWaveformCursor.Visibility = Visibility.Collapsed;
                return;
            }

            var cursorX = GetWaveformCursorX();
            var bucketWidth = width / (double)peaks.Count;
            for (var i = 0; i < peaks.Count; i++)
            {
                var peak = peaks[i];
                var x = i * bucketWidth + bucketWidth / 2d;
                var minY = halfHeight - peak.Min * amplitude * halfHeight;
                var maxY = halfHeight - peak.Max * amplitude * halfHeight;
                var isPlayed = x <= cursorX;
                var stroke = isPlayed
                    ? new SolidColorBrush(Color.FromRgb(122, 247, 255))
                    : new SolidColorBrush(Color.FromRgb(255, 0, 229));

                BackingTrackWaveformCanvas.Children.Add(new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = maxY,
                    Y2 = minY,
                    Stroke = stroke,
                    StrokeThickness = 1.4,
                    SnapsToDevicePixels = true,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                });
            }

            BackingTrackWaveformCursor.Visibility = BackingTrackService.IsLoaded ? Visibility.Visible : Visibility.Collapsed;
            BackingTrackWaveformCursor.X1 = cursorX;
            BackingTrackWaveformCursor.X2 = cursorX;
            BackingTrackWaveformCursor.Y1 = 0;
            BackingTrackWaveformCursor.Y2 = height;
            BackingTrackWaveformCursor.Stroke = new SolidColorBrush(Color.FromRgb(122, 247, 255));
            UpdateLoopSelectionVisual();
        }

        private void UpdateLoopSelectionVisual()
        {
            if (BackingTrackLoopSelection == null || BackingTrackWaveformCanvas == null)
            {
                return;
            }

            var total = BackingTrackService.TotalSeconds;
            if (total <= 0d || _loopSelectionEnd <= _loopSelectionStart)
            {
                BackingTrackLoopSelection.Visibility = Visibility.Collapsed;
                if (BackingTrackTimelineLoopSelection != null)
                {
                    BackingTrackTimelineLoopSelection.Visibility = Visibility.Collapsed;
                }
                return;
            }

            var canvasWidth = Math.Max(1d, BackingTrackWaveformCanvas.ActualWidth);
            var rulerWidth = Math.Max(1d, BackingTrackTimelineCanvas.ActualWidth);
            var startRatio = Math.Clamp(_loopSelectionStart / total, 0d, 1d);
            var endRatio = Math.Clamp(_loopSelectionEnd / total, 0d, 1d);
            var x = canvasWidth * startRatio;
            var width = Math.Max(0d, canvasWidth * (endRatio - startRatio));
            Canvas.SetLeft(BackingTrackLoopSelection, x);
            BackingTrackLoopSelection.Width = width;
            BackingTrackLoopSelection.Height = Math.Max(1d, BackingTrackWaveformCanvas.ActualHeight);
            BackingTrackLoopSelection.Visibility = _loopEnabled ? Visibility.Visible : Visibility.Collapsed;

            if (BackingTrackTimelineLoopSelection != null)
            {
                Canvas.SetLeft(BackingTrackTimelineLoopSelection, rulerWidth * startRatio);
                BackingTrackTimelineLoopSelection.Width = Math.Max(0d, rulerWidth * (endRatio - startRatio));
                BackingTrackTimelineLoopSelection.Height = Math.Max(1d, BackingTrackTimelineCanvas.ActualHeight);
                BackingTrackTimelineLoopSelection.Visibility = _loopEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private double GetWaveformCursorX()
        {
            if (!BackingTrackService.IsLoaded || BackingTrackService.TotalSeconds <= 0)
            {
                return 0d;
            }

            var width = Math.Max(1d, BackingTrackWaveformCanvas.ActualWidth);
            var positionRatio = Math.Clamp(BackingTrackService.PositionSeconds / BackingTrackService.TotalSeconds, 0d, 1d);
            return width * positionRatio;
        }

        private void RefreshBackingTrackUi()
        {
            var loaded = BackingTrackService.IsLoaded;
            var duration = BackingTrackService.TotalSeconds;
            var position = BackingTrackService.PositionSeconds;

            LoadedBackingTrackTextBlock.Text = loaded ? (BackingTrackService.FileName ?? "No track loaded") : (BackingTrackService.LastErrorMessage ?? "No track loaded");
            BackingTrackToggleButton.Content = loaded && BackingTrackService.IsPlaying ? "Pause" : "Play";
            BackingTrackRestartButton.IsEnabled = loaded;
            BackingTrackRewindButton.IsEnabled = loaded;
            BackingTrackToggleButton.IsEnabled = loaded;
            BackingTrackForwardButton.IsEnabled = loaded;
            BackingTrackEndButton.IsEnabled = loaded;
            BackingTrackTimeTextBlock.Text = $"{FormatTime(position)} / {FormatTime(duration)}";

            if (BackingTrackWaveformCanvas != null)
            {
                var canvasWidth = Math.Max(1d, BackingTrackWaveformCanvas.ActualWidth);
                var cursorX = loaded && duration > 0 ? (position / duration) * canvasWidth : 0d;
                BackingTrackWaveformCursor.Visibility = loaded ? Visibility.Visible : Visibility.Collapsed;
                BackingTrackWaveformCursor.X1 = cursorX;
                BackingTrackWaveformCursor.X2 = cursorX;
                BackingTrackWaveformCursor.Y1 = 0;
                BackingTrackWaveformCursor.Y2 = Math.Max(1d, BackingTrackWaveformCanvas.ActualHeight);
                BackingTrackWaveformCursor.Stroke = new SolidColorBrush(Color.FromRgb(122, 247, 255));

                if (loaded && _waveformPeaks.Count > 0)
                {
                    DrawWaveform(_waveformPeaks);
                }
            }

            if (!loaded)
            {
                BackingTrackVolumeSlider.Value = 70;
            }

            DrawTimelineRuler();
            UpdateLoopSelectionVisual();
            CheckLoopBoundary();
        }

        private void ScalesView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsTextInputFocused())
            {
                return;
            }

            if (e.Key == Key.Space)
            {
                e.Handled = true;
                ToggleBackingTrackPlayback();
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Left)
            {
                e.Handled = true;
                JumpToTrackPosition(0d);
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Right)
            {
                e.Handled = true;
                JumpToTrackPosition(BackingTrackService.TotalSeconds);
                return;
            }

            if (e.Key == Key.Left)
            {
                e.Handled = true;
                SeekBackingTrackRelative(-10d);
                return;
            }

            if (e.Key == Key.Right)
            {
                e.Handled = true;
                SeekBackingTrackRelative(10d);
            }
        }

        private void BackingTrackWaveform_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!BackingTrackService.IsLoaded)
            {
                return;
            }

            _isScrubbingWaveform = true;
            e.Handled = true;
            SeekWaveformAtPointer(e.GetPosition(BackingTrackWaveformCanvas));
        }

        private void BackingTrackWaveform_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isScrubbingWaveform || !BackingTrackService.IsLoaded)
            {
                return;
            }

            e.Handled = true;
            SeekWaveformAtPointer(e.GetPosition(BackingTrackWaveformCanvas));
        }

        private void BackingTrackWaveform_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isScrubbingWaveform)
            {
                _isScrubbingWaveform = false;
            }
        }

        private void BackingTrackWaveform_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isScrubbingWaveform = false;
        }

        private void BackingTrackTimelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!BackingTrackService.IsLoaded)
            {
                return;
            }

            var pointer = e.GetPosition(BackingTrackTimelineCanvas);
            var clickedTime = GetTimeAtPointer(pointer.X, BackingTrackTimelineCanvas.ActualWidth, BackingTrackService.TotalSeconds);
            var hasSelection = _loopSelectionEnd > _loopSelectionStart;
            if (!hasSelection || clickedTime < _loopSelectionStart || clickedTime > _loopSelectionEnd)
            {
                ClearLoopSelection();
            }

            _isSelectingLoop = true;
            _loopSelectionStart = SnapToBeatBoundary(clickedTime);
            _loopSelectionEnd = _loopSelectionStart;
            _loopEnabled = true;
            e.Handled = true;
        }

        private void BackingTrackTimelineCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelectingLoop || !BackingTrackService.IsLoaded)
            {
                return;
            }

            var x = e.GetPosition(BackingTrackTimelineCanvas).X;
            var currentTime = GetTimeAtPointer(x, BackingTrackTimelineCanvas.ActualWidth, BackingTrackService.TotalSeconds);
            var snappedCurrent = SnapToBeatBoundary(currentTime);
            var start = _loopSelectionStart;
            var end = snappedCurrent;
            _loopSelectionStart = Math.Min(start, end);
            _loopSelectionEnd = Math.Max(start, end);
            _loopEnabled = true;
            UpdateLoopSelectionVisual();
        }

        private void BackingTrackTimelineCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isSelectingLoop)
            {
                FinalizeLoopSelection();
            }
        }

        private void BackingTrackTimelineCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelectingLoop)
            {
                FinalizeLoopSelection();
            }
        }

        private void FinalizeLoopSelection()
        {
            if (!_isSelectingLoop)
            {
                return;
            }

            _isSelectingLoop = false;
            if (_loopSelectionEnd < _loopSelectionStart)
            {
                var swap = _loopSelectionStart;
                _loopSelectionStart = _loopSelectionEnd;
                _loopSelectionEnd = swap;
            }

            var selectionLength = _loopSelectionEnd - _loopSelectionStart;
            var minimumLoopLength = GetBeatDurationSeconds();
            if (selectionLength < minimumLoopLength)
            {
                var clickedTime = _loopSelectionStart;
                ClearLoopSelection();
                JumpToTrackPosition(clickedTime);
                return;
            }

            _loopSelectionStart = Math.Clamp(_loopSelectionStart, 0d, Math.Max(0d, BackingTrackService.TotalSeconds));
            _loopSelectionEnd = Math.Clamp(_loopSelectionEnd, 0d, Math.Max(0d, BackingTrackService.TotalSeconds));
            _loopEnabled = true;
            JumpToTrackPosition(_loopSelectionStart);
            if (!BackingTrackService.IsPlaying)
            {
                BackingTrackService.Play();
            }
            RefreshBackingTrackUi();
            UpdateLoopSelectionVisual();
        }

        private void ClearLoopSelection()
        {
            _loopSelectionStart = 0d;
            _loopSelectionEnd = 0d;
            _loopEnabled = false;
            UpdateLoopSelectionVisual();
        }

        private int GetTrackBeatsPerMeasure()
        {
            return _trackBeatsPerMeasure switch
            {
                2 => 2,
                3 => 3,
                6 => 6,
                _ => 4
            };
        }

        private double GetBeatDurationSeconds()
        {
            if (_trackBpm <= 0)
            {
                return 0d;
            }

            return 60d / _trackBpm;
        }

        private double GetMeasureDurationSeconds()
        {
            var beatDuration = GetBeatDurationSeconds();
            if (beatDuration <= 0d)
            {
                return 0d;
            }

            return beatDuration * GetTrackBeatsPerMeasure();
        }

        private double SnapToBeatBoundary(double seconds)
        {
            var beatDuration = GetBeatDurationSeconds();
            if (beatDuration <= 0d)
            {
                return Math.Clamp(seconds, 0d, Math.Max(0d, BackingTrackService.TotalSeconds));
            }

            var totalSeconds = Math.Max(0d, BackingTrackService.TotalSeconds);
            var clamped = Math.Clamp(seconds, 0d, totalSeconds);
            var measureDuration = GetMeasureDurationSeconds();
            if (measureDuration <= 0d)
            {
                return clamped;
            }

            var measureIndex = Math.Floor(clamped / measureDuration);
            var measureStart = measureIndex * measureDuration;
            var withinMeasure = clamped - measureStart;
            var beatIndex = Math.Round(withinMeasure / beatDuration);
            var snapped = measureStart + (beatIndex * beatDuration);
            return Math.Clamp(snapped, 0d, totalSeconds);
        }

        private static double GetTimeAtPointer(double x, double width, double totalSeconds)
        {
            if (width <= 0d || totalSeconds <= 0d)
            {
                return 0d;
            }

            return Math.Clamp((x / width) * totalSeconds, 0d, totalSeconds);
        }

        private void SeekWaveformAtPointer(Point pointer)
        {
            if (!BackingTrackService.IsLoaded)
            {
                return;
            }

            var width = Math.Max(1d, BackingTrackWaveformCanvas.ActualWidth);
            var ratio = Math.Clamp(pointer.X / width, 0d, 1d);
            var targetSeconds = ratio * BackingTrackService.TotalSeconds;
            BackingTrackService.Seek(targetSeconds);
            RefreshBackingTrackUi();
        }

        private void ToggleBackingTrackPlayback()
        {
            if (!BackingTrackService.IsLoaded)
            {
                return;
            }

            if (_loopEnabled && _loopSelectionEnd > _loopSelectionStart)
            {
                if (BackingTrackService.IsPlaying)
                {
                    BackingTrackService.Pause();
                    BackingTrackService.Seek(_loopSelectionStart);
                }
                else
                {
                    BackingTrackService.Seek(_loopSelectionStart);
                    BackingTrackService.Play();
                }

                RefreshBackingTrackUi();
                return;
            }

            if (BackingTrackService.IsPlaying)
            {
                BackingTrackService.Pause();
            }
            else
            {
                BackingTrackService.Play();
            }

            RefreshBackingTrackUi();
        }

        private void SeekBackingTrackRelative(double deltaSeconds)
        {
            if (!BackingTrackService.IsLoaded)
            {
                return;
            }

            var nextPosition = Math.Clamp(BackingTrackService.PositionSeconds + deltaSeconds, 0d, Math.Max(BackingTrackService.TotalSeconds, 0d));
            BackingTrackService.Seek(nextPosition);
            RefreshBackingTrackUi();
        }

        private void JumpToTrackPosition(double targetSeconds)
        {
            if (!BackingTrackService.IsLoaded)
            {
                return;
            }

            var clamped = Math.Clamp(targetSeconds, 0d, Math.Max(BackingTrackService.TotalSeconds, 0d));
            BackingTrackService.Seek(clamped);
            RefreshBackingTrackUi();
        }

        private void CheckLoopBoundary()
        {
            if (!_loopEnabled || !BackingTrackService.IsLoaded || _loopSelectionEnd <= _loopSelectionStart)
            {
                return;
            }

            var currentPosition = BackingTrackService.PositionSeconds;
            if (currentPosition >= _loopSelectionEnd)
            {
                BackingTrackService.Seek(_loopSelectionStart);
                if (BackingTrackService.IsPlaying)
                {
                    BackingTrackService.Play();
                }
                RefreshBackingTrackUi();
            }
        }

        private static bool IsTextInputFocused()
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            while (focused != null)
            {
                if (focused is TextBox or PasswordBox or RichTextBox or ComboBox { IsEditable: true })
                {
                    return true;
                }

                focused = VisualTreeHelper.GetParent(focused);
            }

            return false;
        }

        private static string FormatTime(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return timeSpan.TotalHours >= 1
                ? timeSpan.ToString("h\\:mm\\:ss")
                : timeSpan.ToString("mm\\:ss");
        }

        private void UpdateScaleDisplay()
        {
            if (RootNoteComboBox.SelectedItem is not string rootNote || ScaleComboBox.SelectedItem is not string scaleName)
            {
                return;
            }

            var scaleNotes = ScaleCalculator.GetScaleNotes(rootNote, scaleName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var fretboardNotes = ScaleCalculator.GetFretboardDisplay(rootNote, scaleName);

            ScaleSummaryText.Text = ScaleCalculator.BuildScaleSummary(rootNote, scaleName);
            ScaleFretboard.UpdateScaleHighlight(rootNote, fretboardNotes);
        }
    }
}
