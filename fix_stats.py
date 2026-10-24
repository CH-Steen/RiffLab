from pathlib import Path

path = Path(r'C:\Users\ged45\source\repos\GuitarPracticeStudio\GuitarPracticeStudio.App\Views\StatisticsView.xaml')
path.write_text('''<UserControl x:Class="GuitarPracticeStudio.App.Views.StatisticsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             mc:Ignorable="d"
             d:DesignHeight="700"
             d:DesignWidth="900"
             Background="#0E1220">

    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <Border Grid.Row="0"
                CornerRadius="18"
                Background="#111827"
                BorderBrush="#24314B"
                BorderThickness="1"
                Padding="18"
                Margin="0,0,0,18">
            <TextBlock Text="Statistics" Foreground="#F3F8FF" FontSize="28" FontWeight="SemiBold"/>
        </Border>

        <Grid Grid.Row="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <Grid Grid.Row="0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <Border Grid.Column="0" Margin="0,0,12,0" CornerRadius="18" Background="#121A2E" BorderBrush="#24314B" BorderThickness="1" Padding="16">
                    <StackPanel>
                        <TextBlock Text="Practice Time" Foreground="#9FB7D9" FontSize="12"/>
                        <TextBlock x:Name="TotalPracticeTimeText" Text="0h 00m" Foreground="#F3F8FF" FontSize="26" FontWeight="SemiBold" Margin="0,8,0,0"/>
                    </StackPanel>
                </Border>

                <Border Grid.Column="1" Margin="0,0,12,0" CornerRadius="18" Background="#121A2E" BorderBrush="#24314B" BorderThickness="1" Padding="16">
                    <StackPanel>
                        <TextBlock Text="Sessions" Foreground="#9FB7D9" FontSize="12"/>
                        <TextBlock x:Name="TotalSessionsText" Text="0" Foreground="#F3F8FF" FontSize="26" FontWeight="SemiBold" Margin="0,8,0,0"/>
                    </StackPanel>
                </Border>

                <Border Grid.Column="2" Margin="0,0,12,0" CornerRadius="18" Background="#121A2E" BorderBrush="#24314B" BorderThickness="1" Padding="16">
                    <StackPanel>
                        <TextBlock Text="Avg / Day" Foreground="#9FB7D9" FontSize="12"/>
                        <TextBlock x:Name="AverageDailyTimeText" Text="0m" Foreground="#F3F8FF" FontSize="26" FontWeight="SemiBold" Margin="0,8,0,0"/>
                    </StackPanel>
                </Border>

                <Border Grid.Column="3" CornerRadius="18" Background="#121A2E" BorderBrush="#24314B" BorderThickness="1" Padding="16">
                    <StackPanel>
                        <TextBlock Text="XP" Foreground="#9FB7D9" FontSize="12"/>
                        <TextBlock x:Name="TotalXpText" Text="0" Foreground="#F3F8FF" FontSize="26" FontWeight="SemiBold" Margin="0,8,0,0"/>
                    </StackPanel>
                </Border>
            </Grid>

            <Grid Grid.Row="1" Margin="0,18,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="2*"/>
                    <ColumnDefinition Width="1.2*"/>
                </Grid.ColumnDefinitions>

                <Border Grid.Column="0" CornerRadius="22" Background="#101A2D" BorderBrush="#26395F" BorderThickness="1" Padding="18" Margin="0,0,12,0">
                    <StackPanel>
                        <TextBlock Text="7-Day Activity" Foreground="#46E5FF" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,14"/>
                        <Grid x:Name="ActivityChartGrid" Height="190">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="*"/>
                                <RowDefinition Height="Auto"/>
                            </Grid.RowDefinitions>
                        </Grid>
                    </StackPanel>
                </Border>

                <Border Grid.Column="1" CornerRadius="22" Background="#101A2D" BorderBrush="#26395F" BorderThickness="1" Padding="18">
                    <StackPanel>
                        <TextBlock Text="Key Metrics" Foreground="#46E5FF" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,12"/>

                        <TextBlock Text="Current streak" Foreground="#A8B8D6" FontSize="12" Margin="0,0,0,4"/>
                        <TextBlock x:Name="CurrentStreakText" Text="0 days" Foreground="#F3F8FF" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,12"/>

                        <TextBlock Text="Longest streak" Foreground="#A8B8D6" FontSize="12" Margin="0,0,0,4"/>
                        <TextBlock x:Name="LongestStreakText" Text="0 days" Foreground="#F3F8FF" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,12"/>

                        <TextBlock Text="Longest session" Foreground="#A8B8D6" FontSize="12" Margin="0,0,0,4"/>
                        <TextBlock x:Name="LongestSessionText" Text="0 min" Foreground="#F3F8FF" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,12"/>

                        <TextBlock Text="Most practiced" Foreground="#A8B8D6" FontSize="12" Margin="0,0,0,4"/>
                        <TextBlock x:Name="MostPracticedText" Text="—" Foreground="#F3F8FF" FontSize="18" FontWeight="SemiBold"/>
                    </StackPanel>
                </Border>
            </Grid>
        </Grid>
    </Grid>
</UserControl>
''', encoding='utf-8')

xaml_cs_path = Path(r'C:\Users\ged45\source\repos\GuitarPracticeStudio\GuitarPracticeStudio.App\Views\StatisticsView.xaml.cs')
xaml_cs_path.write_text('''using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    }
}
''', encoding='utf-8')

practice_path = Path(r'C:\Users\ged45\source\repos\GuitarPracticeStudio\GuitarPracticeStudio.App\Views\PracticeView.xaml.cs')
practice_text = practice_path.read_text(encoding='utf-8')
needle = 'ExerciseComboBox.DisplayMemberPath = nameof(ExerciseDefinition.Name);'
if needle in practice_text and 'SelectedValuePath' not in practice_text:
    practice_text = practice_text.replace(needle, needle + '\n            ExerciseComboBox.SelectedValuePath = nameof(ExerciseDefinition.Name);')
    practice_path.write_text(practice_text, encoding='utf-8')

print('done')
