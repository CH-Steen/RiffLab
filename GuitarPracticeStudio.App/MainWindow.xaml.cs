using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RiffLab.App.Services;
using RiffLab.App.Views;

namespace RiffLab.App
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Button, UserControl> _pages = new();
        private Button? _selectedButton;

        public MainWindow()
        {
            InitializeComponent();
            AppThemeManager.Initialize();
            PracticeSessionService.Instance.LoadSessions();
            InitializeNavigation();
            SelectPage(DashboardButton, new DashboardView());
        }

        public void NavigateToPractice(string? selectedExerciseName = null)
        {
            if (!_pages.TryGetValue(PracticeButton, out var page))
            {
                return;
            }

            SelectPage(PracticeButton, page);

            if (page is PracticeView practiceView)
            {
                practiceView.SetSelectedExercise(selectedExerciseName);
            }
        }

        public void RefreshAllPageData()
        {
            foreach (var page in _pages.Values)
            {
                if (page is CalendarView calendarView)
                {
                    calendarView.RefreshCalendar();
                }

                if (page is StatisticsView statisticsView)
                {
                    statisticsView.RefreshStatistics();
                }
            }
        }

        private void InitializeNavigation()
        {
            _pages[DashboardButton] = new DashboardView();
            _pages[PracticeButton] = new PracticeView();
            _pages[ExercisesButton] = new ExercisesView();
            _pages[ScalesButton] = new ScalesView();
            _pages[CalendarButton] = new CalendarView();
            _pages[StatisticsButton] = new StatisticsView();
            _pages[SettingsButton] = new SettingsView();

            foreach (var button in _pages.Keys)
            {
                button.Click += (sender, _) =>
                {
                    if (sender is Button clickedButton && _pages.TryGetValue(clickedButton, out var page))
                    {
                        SelectPage(clickedButton, page);
                    }
                };
            }
        }

        private void SelectPage(Button selectedButton, UserControl? page = null)
        {
            if (_selectedButton != null && _selectedButton != selectedButton)
            {
                ResetButtonStyle(_selectedButton);
            }

            _selectedButton = selectedButton;
            ApplySelectedStyle(_selectedButton);

            if (page != null)
            {
                PageHost.Content = page;

                if (page is DashboardView dashboardView)
                {
                    dashboardView.RefreshDashboard();
                }

                        if (page is CalendarView calendarView)
                        {
                            calendarView.RefreshCalendar();
                        }

                        if (page is StatisticsView statisticsView)
                        {
                            statisticsView.RefreshStatistics();
                        }
                    }
                }

        private static void ResetButtonStyle(Button button)
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 42, 63));
            button.Foreground = new SolidColorBrush(Color.FromRgb(185, 201, 233));
        }

        private static void ApplySelectedStyle(Button button)
        {
            button.Background = new SolidColorBrush(Color.FromRgb(18, 26, 46));
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(70, 229, 255));
            button.Foreground = new SolidColorBrush(Color.FromRgb(240, 248, 255));
        }
    }
}