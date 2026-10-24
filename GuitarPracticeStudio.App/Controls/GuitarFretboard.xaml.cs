using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace RiffLab.App.Controls
{
    public partial class GuitarFretboard : UserControl
    {
        private const int StringCount = 6;
        private const int MaxFret = 12;
        private readonly string[] _stringNotes = { "e", "B", "G", "D", "A", "E" };
        private readonly Dictionary<(int StringIndex, int Fret), bool> _selectedPoints = new();
        private readonly HashSet<(int StringIndex, int Fret)> _scaleHighlights = new();
        private string _rootNote = "A";
        private double _stringSpacing;
        private double _fretWidth;

        public GuitarFretboard()
        {
            InitializeComponent();
            Loaded += (_, _) => BuildBoard();
            SizeChanged += (_, _) => BuildBoard();
        }

        public IReadOnlyCollection<(int StringIndex, int Fret)> SelectedPositions => _selectedPoints.Keys.ToList().AsReadOnly();

        public void UpdateScaleHighlight(string rootNote, IReadOnlyList<(int StringIndex, int Fret, string NoteName, bool IsInScale, bool IsRoot)> scalePositions)
        {
            _rootNote = rootNote;
            _scaleHighlights.Clear();

            foreach (var position in scalePositions)
            {
                if (position.IsInScale || position.IsRoot)
                {
                    _scaleHighlights.Add((position.StringIndex, position.Fret));
                }
            }

            BuildBoard();
        }

        public void ClearSelection()
        {
            _selectedPoints.Clear();
            _scaleHighlights.Clear();
            BuildBoard();
        }

        public void ToggleSelection(int stringIndex, int fret)
        {
            if (stringIndex < 0 || stringIndex >= StringCount || fret < 0 || fret > MaxFret)
            {
                return;
            }

            var key = (stringIndex, fret);
            if (_selectedPoints.ContainsKey(key))
            {
                _selectedPoints.Remove(key);
            }
            else
            {
                _selectedPoints[key] = true;
            }

            BuildBoard();
        }

        private static DropShadowEffect CreateGlow(Color color, double blurRadius = 16, double opacity = 0.72)
        {
            return new DropShadowEffect
            {
                Color = color,
                ShadowDepth = 0,
                BlurRadius = blurRadius,
                Opacity = opacity
            };
        }

        private void BuildBoard()
        {
            RootGrid.Children.Clear();
            RootGrid.ColumnDefinitions.Clear();
            RootGrid.RowDefinitions.Clear();

            var width = ActualWidth > 0 ? ActualWidth : 820;
            var height = ActualHeight > 0 ? ActualHeight : 280;
            const double fretNumberRowHeight = 24;
            var boardHeight = Math.Max(220, (height - fretNumberRowHeight - 16) * 0.82);

            _stringSpacing = boardHeight / StringCount;
            _fretWidth = Math.Max(52, (width - 68) / MaxFret);

            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52, GridUnitType.Pixel) });
            for (var i = 0; i < MaxFret; i++)
            {
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_fretWidth, GridUnitType.Pixel) });
            }

            for (var i = 0; i < StringCount; i++)
            {
                RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(_stringSpacing, GridUnitType.Pixel) });
            }

            RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(fretNumberRowHeight, GridUnitType.Pixel) });

            var background = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(8, 12, 24)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(44, 62, 104)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(16),
                Opacity = 1,
            };
            Grid.SetColumnSpan(background, MaxFret + 1);
            Grid.SetRowSpan(background, StringCount);
            RootGrid.Children.Add(background);

            for (var fret = 0; fret <= MaxFret; fret++)
            {
                var fretLine = new Border
                {
                    Background = new SolidColorBrush(fret == 0 ? Color.FromRgb(134, 148, 178) : Color.FromRgb(82, 96, 132)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(164, 180, 212)),
                    BorderThickness = new Thickness(fret == 0 ? 2.5 : 1, 0, 0, 0),
                    Width = fret == 0 ? 52 : _fretWidth,
                    Opacity = 1,
                };
                Grid.SetRowSpan(fretLine, StringCount);
                Grid.SetColumn(fretLine, fret + 1);
                RootGrid.Children.Add(fretLine);
            }

            for (var stringIndex = 0; stringIndex < StringCount; stringIndex++)
            {
                var stringLine = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(224, 234, 255)),
                    CornerRadius = new CornerRadius(2),
                    Height = 2,
                    Margin = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 1
                };
                Grid.SetColumnSpan(stringLine, MaxFret + 1);
                Grid.SetRow(stringLine, stringIndex);
                RootGrid.Children.Add(stringLine);

                var note = new TextBlock
                {
                    Text = _stringNotes[stringIndex],
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 245, 255)),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(8, 0, 0, 0),
                    ToolTip = $"String {_stringNotes[stringIndex]}"
                };
                Grid.SetColumn(note, 0);
                Grid.SetRow(note, stringIndex);
                RootGrid.Children.Add(note);
            }

            for (var fret = 0; fret <= MaxFret; fret++)
            {
                var fretNumber = new TextBlock
                {
                    Text = fret.ToString(),
                    Foreground = new SolidColorBrush(Color.FromRgb(216, 230, 255)),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0),
                    Opacity = 0.95
                };

                Grid.SetColumn(fretNumber, fret + 1);
                Grid.SetRow(fretNumber, StringCount);
                RootGrid.Children.Add(fretNumber);
            }

            for (var stringIndex = 0; stringIndex < StringCount; stringIndex++)
            {
                for (var fret = 0; fret <= MaxFret; fret++)
                {
                    var noteName = GetNoteNameForPosition(stringIndex, fret);
                    var isSelected = _selectedPoints.ContainsKey((stringIndex, fret));
                    var isScaleNote = _scaleHighlights.Contains((stringIndex, fret));
                    var isRootNote = string.Equals(noteName, _rootNote, StringComparison.OrdinalIgnoreCase);
                    var showLabel = isScaleNote || isSelected || isRootNote;
                    var fillColor = isRootNote ? Color.FromRgb(0, 245, 255) : isScaleNote ? Color.FromRgb(255, 0, 229) : Color.FromRgb(10, 17, 28);
                    var borderColor = isRootNote ? Color.FromRgb(120, 249, 255) : isScaleNote ? Color.FromRgb(255, 103, 245) : Color.FromRgb(84, 102, 146);
                    var glowColor = isRootNote ? Color.FromRgb(0, 245, 255) : isScaleNote ? Color.FromRgb(255, 0, 229) : Color.FromRgb(84, 102, 146);
                    var textColor = isRootNote ? Color.FromRgb(8, 12, 24) : Color.FromRgb(243, 248, 255);
                    var textShadow = isRootNote ? null : new DropShadowEffect
                    {
                        Color = Color.FromRgb(8, 12, 24),
                        ShadowDepth = 0,
                        BlurRadius = 2,
                        Opacity = 0.65
                    };

                    var noteSize = 28d;
                    var position = new Border
                    {
                        Background = new SolidColorBrush(fillColor),
                        BorderBrush = new SolidColorBrush(borderColor),
                        BorderThickness = new Thickness(1.5),
                        CornerRadius = new CornerRadius(10),
                        Width = noteSize,
                        Height = noteSize,
                        Margin = new Thickness(0),
                        Cursor = Cursors.Hand,
                        ToolTip = $"String {_stringNotes[stringIndex]} / Fret {fret} / Note {noteName}",
                        Opacity = showLabel ? 1 : 0.12,
                        Effect = showLabel ? CreateGlow(glowColor, 12, 0.72) : null,
                        Child = new TextBlock
                        {
                            Text = showLabel ? noteName : string.Empty,
                            Foreground = new SolidColorBrush(textColor),
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Effect = textShadow
                        }
                    };

                    position.MouseLeftButtonDown += (_, _) => ToggleSelection(stringIndex, fret);
                    position.Tag = (stringIndex, fret);

                    Grid.SetColumn(position, fret + 1);
                    Grid.SetRow(position, stringIndex);
                    RootGrid.Children.Add(position);
                }
            }
        }

        private static string GetNoteNameForPosition(int stringIndex, int fret)
        {
            var noteOffsets = new[] { 4, 9, 2, 7, 11, 4 };
            var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            var pitch = (noteOffsets[stringIndex] + fret) % 12;
            return noteNames[pitch];
        }
    }
}
