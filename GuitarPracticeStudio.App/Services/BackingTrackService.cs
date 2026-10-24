using System;
using System.Diagnostics;
using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace RiffLab.App.Services
{
    public static class BackingTrackService
    {
        private static WaveStream? _reader;
        private static IWavePlayer? _output;
        private static string? _filePath;
        private static double _volume = 0.7d;

        public static event Action? StateChanged;

        public static bool IsLoaded => _reader != null && _output != null;
        public static bool IsPlaying => _output is { PlaybackState: PlaybackState.Playing };
        public static string? FilePath => _filePath;
        public static string? FileName => string.IsNullOrWhiteSpace(_filePath) ? null : Path.GetFileName(_filePath);
        public static double PositionSeconds => _reader?.CurrentTime.TotalSeconds ?? 0d;
        public static double TotalSeconds => _reader?.TotalTime.TotalSeconds ?? 0d;
        public static string? LastErrorMessage { get; private set; }

        public static bool Load(string path)
        {
            LastErrorMessage = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                LastErrorMessage = "Please choose a valid MP3 or WAV file.";
                StateChanged?.Invoke();
                return false;
            }

            if (!File.Exists(path))
            {
                LastErrorMessage = "The selected audio file could not be found.";
                StateChanged?.Invoke();
                return false;
            }

            var extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
            {
                LastErrorMessage = "Unsupported file type. Please choose an MP3 or WAV track.";
                StateChanged?.Invoke();
                return false;
            }

            WaveStream? reader = null;
            IWavePlayer? output = null;
            try
            {
                ReleaseCurrentTrack();
                reader = new AudioFileReader(path);
                output = CreateOutputDevice(AudioOutputService.Instance.SelectedDeviceId);
                output.Volume = (float)Math.Clamp(_volume, 0d, 1d);

                output.Init(reader);
                _filePath = path;
                _reader = reader;
                _output = output;
                StateChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                output?.Dispose();
                reader?.Dispose();
                _reader = null;
                _output = null;
                _filePath = null;

                var detail = $"{ex.GetType().Name}: {ex.Message}";
                Debug.WriteLine($"BackingTrackService.Load failed for '{path}': {detail}{Environment.NewLine}{ex}");
                LastErrorMessage = $"Unable to load '{Path.GetFileName(path)}'. {detail}. Please choose a valid MP3 or WAV track.";
                StateChanged?.Invoke();
                return false;
            }
        }

        public static bool TryLoad(string path)
        {
            return Load(path);
        }

        private static IWavePlayer CreateOutputDevice(string? selectedDeviceId)
        {
            var deviceId = string.IsNullOrWhiteSpace(selectedDeviceId) ? AudioOutputService.DefaultDeviceId : selectedDeviceId;

            if (string.Equals(deviceId, AudioOutputService.DefaultDeviceId, StringComparison.Ordinal))
            {
                return new WasapiOut(AudioClientShareMode.Shared, 0);
            }

            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .FirstOrDefault(x => string.Equals(x.ID, deviceId, StringComparison.Ordinal));

                return device == null
                    ? new WasapiOut(AudioClientShareMode.Shared, 0)
                    : new WasapiOut(device, AudioClientShareMode.Shared, false, 0);
            }
            catch (Exception)
            {
                return new WasapiOut(AudioClientShareMode.Shared, 0);
            }
        }

        private static void ReleaseCurrentTrack()
        {
            if (_output != null)
            {
                _output.Stop();
                _output.Dispose();
            }

            if (_reader != null)
            {
                _reader.Dispose();
            }

            _output = null;
            _reader = null;
            _filePath = null;
        }

        public static void Play()
        {
            if (_output == null || _reader == null)
            {
                return;
            }

            if (_output.PlaybackState != PlaybackState.Playing)
            {
                _output.Play();
            }

            StateChanged?.Invoke();
        }

        public static void Pause()
        {
            if (_output != null && _output.PlaybackState == PlaybackState.Playing)
            {
                _output.Pause();
            }

            StateChanged?.Invoke();
        }

        public static void Stop()
        {
            if (_output != null && _output.PlaybackState != PlaybackState.Stopped)
            {
                _output.Stop();
            }

            if (_reader != null)
            {
                _reader.CurrentTime = TimeSpan.Zero;
            }

            StateChanged?.Invoke();
        }

        public static void Seek(double seconds)
        {
            if (_reader == null)
            {
                return;
            }

            var total = TotalSeconds;
            if (total <= 0)
            {
                return;
            }

            _reader.CurrentTime = TimeSpan.FromSeconds(Math.Clamp(seconds, 0d, total));
            StateChanged?.Invoke();
        }

        public static void SetVolume(double value)
        {
            _volume = Math.Clamp(value, 0d, 1d);

            if (_output != null)
            {
                _output.Volume = (float)_volume;
            }

            StateChanged?.Invoke();
        }

        public static void Dispose()
        {
            ReleaseCurrentTrack();
            LastErrorMessage = null;
            StateChanged?.Invoke();
        }
    }
}
