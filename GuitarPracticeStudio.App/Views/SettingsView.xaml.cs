using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using GuitarPracticeStudio.App;
using GuitarPracticeStudio.App.Services;

namespace GuitarPracticeStudio.App.Views
{
    public partial class SettingsView : UserControl
    {
        private const string SettingsFileName = "settings.json";
        private readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GuitarPracticeStudio",
            SettingsFileName);

        public SettingsView()
        {
            InitializeComponent();
            InitializeThemeOptions();
            LoadOutputDevices();
            RestoreSelectedOutputDevice();
            RestoreSelectedTheme();
        }

        private void InitializeThemeOptions()
        {
            ThemeComboBox.ItemsSource = new[]
            {
                "Cyberpunk",
                "Windows Dark",
                "Windows Light"
            };
        }

        private void RestoreSelectedTheme()
        {
            var theme = AppThemeManager.LoadTheme();
            var displayValue = theme switch
            {
                AppTheme.WindowsDark => "Windows Dark",
                AppTheme.WindowsLight => "Windows Light",
                _ => "Cyberpunk"
            };

            ThemeComboBox.SelectedItem = displayValue;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is not string selectedTheme)
            {
                return;
            }

            var appTheme = selectedTheme switch
            {
                "Windows Dark" => AppTheme.WindowsDark,
                "Windows Light" => AppTheme.WindowsLight,
                _ => AppTheme.Cyberpunk
            };

            AppThemeManager.ApplyTheme(appTheme);
            AppThemeManager.SaveTheme(appTheme);
        }

        private void LoadOutputDevices()
        {
            var devices = AudioOutputService.Instance.GetOutputDevices();
            AudioOutputDeviceComboBox.ItemsSource = devices;
            AudioOutputDeviceComboBox.DisplayMemberPath = nameof(AudioDeviceInfo.Name);
            AudioOutputDeviceComboBox.SelectedValuePath = nameof(AudioDeviceInfo.Id);
        }

        private void RestoreSelectedOutputDevice()
        {
            var selectedId = LoadSelectedOutputDeviceId();
            var devices = AudioOutputDeviceComboBox.ItemsSource as IEnumerable;
            if (devices == null)
            {
                return;
            }

            var selectedDevice = devices.Cast<AudioDeviceInfo>().FirstOrDefault(device => string.Equals(device.Id, selectedId, StringComparison.Ordinal));
            if (selectedDevice == null)
            {
                selectedDevice = AudioOutputService.Instance.GetOutputDevices().FirstOrDefault(device => device.Id == "__DEFAULT__") ?? AudioOutputService.Instance.GetOutputDevices().FirstOrDefault();
            }

            if (selectedDevice != null)
            {
                AudioOutputDeviceComboBox.SelectedItem = selectedDevice;
                AudioOutputService.Instance.SelectedDeviceId = selectedDevice.Id;
            }

            AudioOutputDeviceComboBox.SelectionChanged += AudioOutputDeviceComboBox_SelectionChanged;
        }

        private void AudioOutputDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AudioOutputDeviceComboBox.SelectedItem is AudioDeviceInfo device)
            {
                AudioOutputService.Instance.SelectedDeviceId = device.Id;
                SaveSelectedOutputDeviceId(device.Id);
            }
        }

        private void TestSoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (AudioOutputDeviceComboBox.SelectedItem is AudioDeviceInfo device)
            {
                AudioOutputService.Instance.SelectedDeviceId = device.Id;
                SaveSelectedOutputDeviceId(device.Id);
            }

            try
            {
                AudioOutputService.Instance.PlayTestTone();
            }
            catch
            {
            }
        }

        private string? LoadSelectedOutputDeviceId()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    return null;
                }

                var json = File.ReadAllText(_settingsPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                var settings = JsonSerializer.Deserialize<SettingsState>(json);
                return settings?.SelectedAudioOutputDeviceId;
            }
            catch
            {
                return null;
            }
        }

        private void SaveSelectedOutputDeviceId(string deviceId)
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var settings = new SettingsState
                {
                    SelectedAudioOutputDeviceId = deviceId
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
            }
        }

        private sealed class SettingsState
        {
            public string? SelectedAudioOutputDeviceId { get; set; }
        }
    }
}
