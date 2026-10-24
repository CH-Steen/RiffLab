using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GuitarPracticeStudio.App.Services;

namespace GuitarPracticeStudio.App
{
    public enum AppTheme
    {
        Cyberpunk,
        WindowsDark,
        WindowsLight
    }

    public static class AppThemeManager
    {
        private const string SettingsFileName = "settings.json";

        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Cyberpunk;

        public static void Initialize()
        {
            ApplyTheme(LoadTheme());
        }

        public static AppTheme LoadTheme()
        {
            try
            {
                var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GuitarPracticeStudio");
                Directory.CreateDirectory(appDataFolder);
                var settingsPath = Path.Combine(appDataFolder, SettingsFileName);

                if (!File.Exists(settingsPath))
                {
                    return AppTheme.Cyberpunk;
                }

                var json = File.ReadAllText(settingsPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return AppTheme.Cyberpunk;
                }

                var state = JsonSerializer.Deserialize<SettingsState>(json);
                if (state == null)
                {
                    return AppTheme.Cyberpunk;
                }

                if (Enum.TryParse<AppTheme>(state.SelectedTheme, true, out var theme))
                {
                    return theme;
                }
            }
            catch
            {
            }

            return AppTheme.Cyberpunk;
        }

        public static void ApplyTheme(AppTheme theme)
        {
            CurrentTheme = theme;

            var appResources = Application.Current.Resources;
            switch (theme)
            {
                case AppTheme.WindowsDark:
                    appResources["AppWindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(22, 24, 28));
                    appResources["AppPanelBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 32, 36));
                    appResources["AppPanelAltBrush"] = new SolidColorBrush(Color.FromRgb(38, 41, 46));
                    appResources["AppPrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                    appResources["AppSecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(180, 188, 202));
                    appResources["AppBorderBrush"] = new SolidColorBrush(Color.FromRgb(76, 89, 104));
                    appResources["AppAccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                    appResources["AppAccentSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(138, 92, 246));
                    appResources["AppInputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(28, 31, 35));
                    break;
                case AppTheme.WindowsLight:
                    appResources["AppWindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(245, 247, 250));
                    appResources["AppPanelBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    appResources["AppPanelAltBrush"] = new SolidColorBrush(Color.FromRgb(240, 243, 248));
                    appResources["AppPrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(20, 28, 38));
                    appResources["AppSecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(66, 78, 96));
                    appResources["AppBorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 214, 226));
                    appResources["AppAccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                    appResources["AppAccentSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(124, 105, 255));
                    appResources["AppInputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                    break;
                default:
                    appResources["AppWindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(9, 11, 18));
                    appResources["AppPanelBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(14, 18, 32));
                    appResources["AppPanelAltBrush"] = new SolidColorBrush(Color.FromRgb(18, 26, 46));
                    appResources["AppPrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(243, 248, 255));
                    appResources["AppSecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(168, 184, 214));
                    appResources["AppBorderBrush"] = new SolidColorBrush(Color.FromRgb(43, 59, 93));
                    appResources["AppAccentBrush"] = new SolidColorBrush(Color.FromRgb(70, 229, 255));
                    appResources["AppAccentSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(155, 123, 255));
                    appResources["AppInputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(18, 26, 46));
                    break;
            }

            foreach (var window in Application.Current.Windows.OfType<Window>())
            {
                ApplyThemeToVisualTree(window);
            }
        }

        public static void SaveTheme(AppTheme theme)
        {
            try
            {
                var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GuitarPracticeStudio");
                Directory.CreateDirectory(appDataFolder);
                var settingsPath = Path.Combine(appDataFolder, SettingsFileName);
                var settings = LoadSettings();
                settings.SelectedTheme = theme.ToString();
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
            }
        }

        private static SettingsState LoadSettings()
        {
            var settings = new SettingsState();
            try
            {
                var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GuitarPracticeStudio");
                Directory.CreateDirectory(appDataFolder);
                var settingsPath = Path.Combine(appDataFolder, SettingsFileName);

                if (!File.Exists(settingsPath))
                {
                    return settings;
                }

                var json = File.ReadAllText(settingsPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return settings;
                }

                var loaded = JsonSerializer.Deserialize<SettingsState>(json);
                if (loaded != null)
                {
                    settings = loaded;
                }
            }
            catch
            {
            }

            return settings;
        }

        private static void ApplyThemeToVisualTree(DependencyObject root)
        {
            if (root is Window window)
            {
                window.Background = (Brush)Application.Current.Resources["AppWindowBackgroundBrush"];
                window.Foreground = (Brush)Application.Current.Resources["AppPrimaryTextBrush"];
            }

            if (root is Border border)
            {
                if (border.Background is SolidColorBrush backgroundBrush && backgroundBrush.Color == Color.FromRgb(14, 18, 32))
                {
                    border.Background = (Brush)Application.Current.Resources["AppPanelBackgroundBrush"];
                }
                else if (border.Background is SolidColorBrush alt && alt.Color == Color.FromRgb(18, 26, 46))
                {
                    border.Background = (Brush)Application.Current.Resources["AppPanelAltBrush"];
                }
            }

            if (root is TextBlock textBlock && textBlock.Foreground is SolidColorBrush textBrush)
            {
                if (textBrush.Color == Color.FromRgb(243, 248, 255) || textBrush.Color == Color.FromRgb(168, 184, 214) || textBrush.Color == Color.FromRgb(185, 201, 233))
                {
                    textBlock.Foreground = (Brush)Application.Current.Resources["AppPrimaryTextBrush"];
                }
            }

            if (root is Button button)
            {
                if (button.Background is SolidColorBrush bgBrush && (bgBrush.Color == Color.FromRgb(18, 26, 46) || bgBrush.Color == Color.FromRgb(12, 18, 31)))
                {
                    button.Background = (Brush)Application.Current.Resources["AppPanelAltBrush"];
                }

                if (button.BorderBrush is SolidColorBrush borderBrush && borderBrush.Color == Color.FromRgb(30, 42, 63))
                {
                    button.BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];
                }
            }

            if (root is ComboBox comboBox)
            {
                if (comboBox.Background is SolidColorBrush bgBrush && bgBrush.Color == Color.FromRgb(15, 21, 35))
                {
                    comboBox.Background = (Brush)Application.Current.Resources["AppInputBackgroundBrush"];
                }

                if (comboBox.BorderBrush is SolidColorBrush borderBrush && borderBrush.Color == Color.FromRgb(43, 59, 93))
                {
                    comboBox.BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];
                }

                if (comboBox.Foreground is SolidColorBrush fg && fg.Color == Color.FromRgb(243, 248, 255))
                {
                    comboBox.Foreground = (Brush)Application.Current.Resources["AppPrimaryTextBrush"];
                }
            }

            if (root is TextBox textBox)
            {
                if (textBox.Background is SolidColorBrush bgBrush && bgBrush.Color == Color.FromRgb(18, 26, 46))
                {
                    textBox.Background = (Brush)Application.Current.Resources["AppInputBackgroundBrush"];
                }

                if (textBox.BorderBrush is SolidColorBrush borderBrush && borderBrush.Color == Color.FromRgb(43, 59, 93))
                {
                    textBox.BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];
                }

                if (textBox.Foreground is SolidColorBrush fg && fg.Color == Color.FromRgb(243, 248, 255))
                {
                    textBox.Foreground = (Brush)Application.Current.Resources["AppPrimaryTextBrush"];
                }
            }

            if (root is DatePicker datePicker)
            {
                datePicker.Background = (Brush)Application.Current.Resources["AppInputBackgroundBrush"];
                datePicker.BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];
                datePicker.Foreground = (Brush)Application.Current.Resources["AppPrimaryTextBrush"];
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                ApplyThemeToVisualTree(child);
            }
        }

        private sealed class SettingsState
        {
            public string SelectedTheme { get; set; } = AppTheme.Cyberpunk.ToString();
            public string? SelectedAudioOutputDeviceId { get; set; }
        }
    }

    public partial class App : Application
    {
        public App()
        {
            Exit += (_, _) =>
            {
                BackingTrackService.Dispose();
                SharedMetronomeService.Stop();
            };
        }
    }
}
