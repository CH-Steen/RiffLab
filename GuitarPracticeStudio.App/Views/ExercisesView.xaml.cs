using System.Windows;
using System.Windows.Controls;
using RiffLab.App.Models;
using RiffLab.App.Services;

namespace RiffLab.App.Views
{
    public partial class ExercisesView : UserControl
    {
        private int? _editingExerciseId;

        public ExercisesView()
        {
            InitializeComponent();
            Loaded += ExercisesView_Loaded;
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;
            CategoryFilterComboBox.SelectionChanged += FilterChanged;
            DifficultyFilterComboBox.SelectionChanged += FilterChanged;
            InitializeFilters();
            ResetCustomForm();
        }

        private void ExercisesView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshExercises();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshExercises();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshExercises();
        }

        private void InitializeFilters()
        {
            CategoryFilterComboBox.ItemsSource = ExerciseLibraryService.Instance.GetCategories();
            DifficultyFilterComboBox.ItemsSource = ExerciseLibraryService.Instance.GetDifficulties();
            ExerciseCategoryComboBox.ItemsSource = ExerciseLibraryService.Instance.GetCategories().Where(item => !item.Equals("All", StringComparison.OrdinalIgnoreCase)).ToList();
            ExerciseDifficultyComboBox.ItemsSource = ExerciseLibraryService.Instance.GetDifficulties().Where(item => !item.Equals("All", StringComparison.OrdinalIgnoreCase)).ToList();
            CategoryFilterComboBox.SelectedIndex = 0;
            DifficultyFilterComboBox.SelectedIndex = 0;
            ExerciseCategoryComboBox.SelectedIndex = 0;
            ExerciseDifficultyComboBox.SelectedIndex = 0;
        }

        private void CreateCustomExerciseButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCustomForm();
        }

        private void SaveCustomExerciseButton_Click(object sender, RoutedEventArgs e)
        {
            var name = ExerciseNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Exercise name is required.", "Missing info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var category = ExerciseCategoryComboBox.SelectedItem?.ToString() ?? "Warmups";
            var difficulty = ExerciseDifficultyComboBox.SelectedItem?.ToString() ?? "Beginner";
            var description = $"Custom exercise focused on {category.ToLowerInvariant()} practice.";

            var exercise = new ExerciseDefinition
            {
                Name = name,
                Category = category,
                Difficulty = difficulty,
                Description = description,
                IsCustom = true
            };

            if (_editingExerciseId.HasValue)
            {
                exercise.Id = _editingExerciseId.Value;
                ExerciseLibraryService.Instance.UpdateCustomExercise(exercise);
            }
            else
            {
                ExerciseLibraryService.Instance.AddCustomExercise(exercise);
            }

            ResetCustomForm();
            RefreshExercises();
        }

        private void CancelCustomExerciseButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCustomForm();
        }

        private void EditExerciseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ExerciseDefinition exercise)
            {
                _editingExerciseId = exercise.Id;
                ExerciseNameTextBox.Text = exercise.Name;
                ExerciseCategoryComboBox.SelectedItem = exercise.Category;
                ExerciseDifficultyComboBox.SelectedItem = exercise.Difficulty;
            }
        }

        private void DeleteExerciseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ExerciseDefinition exercise)
            {
                var result = MessageBox.Show($"Delete '{exercise.Name}'?", "Delete custom exercise", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ExerciseLibraryService.Instance.DeleteCustomExercise(exercise.Id);
                    RefreshExercises();
                }
            }
        }

        private void PracticeExerciseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ExerciseDefinition exercise)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToPractice(exercise.Name);
            }
        }

        private void RefreshExercises()
        {
            var search = SearchTextBox.Text;
            var category = CategoryFilterComboBox.SelectedItem?.ToString() ?? "All";
            var difficulty = DifficultyFilterComboBox.SelectedItem?.ToString() ?? "All";

            var exercises = ExerciseLibraryService.Instance.GetFilteredExercises(search, category, difficulty);
            ExercisesList.ItemsSource = exercises;
        }

        private void ResetCustomForm()
        {
            _editingExerciseId = null;
            ExerciseNameTextBox.Text = string.Empty;
            ExerciseCategoryComboBox.SelectedIndex = 0;
            ExerciseDifficultyComboBox.SelectedIndex = 0;
        }
    }
}
