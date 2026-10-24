using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace RiffLab.App.Services
{
    public sealed class AudioOutputService : IDisposable
    {
        public const string DefaultDeviceId = "__DEFAULT__";

        private readonly object _lock = new();
        private IWavePlayer? _player;
        private string? _selectedDeviceId;

        public static AudioOutputService Instance { get; } = new();

        public IReadOnlyList<AudioDeviceInfo> GetOutputDevices()
        {
            var devices = new List<AudioDeviceInfo>
            {
                new(DefaultDeviceId, "Windows Default Output")
            };

            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var mmDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .OrderBy(device => device.FriendlyName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var device in mmDevices)
                {
                    devices.Add(new AudioDeviceInfo(device.ID, device.FriendlyName));
                }
            }
            catch
            {
                // Gracefully fall back to the default output only when enumeration is unavailable.
            }

            return devices;
        }

        public string? SelectedDeviceId
        {
            get
            {
                lock (_lock)
                {
                    return _selectedDeviceId ?? DefaultDeviceId;
                }
            }
            set
            {
                lock (_lock)
                {
                    _selectedDeviceId = string.IsNullOrWhiteSpace(value) ? DefaultDeviceId : value;
                }
            }
        }

        public void PlayTestTone()
        {
            var tone = CreateToneWave(880f, 0.18d, 0.25d);
            PlayWave(tone, SelectedDeviceId);
        }

        public void PlayMetronomeClick(bool accent, double volume)
        {
            var frequency = accent ? 1800f : 1200f;
            var amplitude = accent ? 0.95d : 0.55d;
            var tone = CreateToneWave(frequency, Math.Clamp(volume, 0.0d, 1.0d) * amplitude, 0.05d);
            PlayWave(tone, SelectedDeviceId);
        }

        public void Stop()
        {
            lock (_lock)
            {
                _player?.Stop();
                _player?.Dispose();
                _player = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void PlayWave(byte[] waveData, string? deviceId)
        {
            try
            {
                Stop();

                var stream = new MemoryStream(waveData, writable: false);
                var reader = new WaveFileReader(stream);

                lock (_lock)
                {
                    if (DefaultDeviceId.Equals(deviceId, StringComparison.Ordinal))
                    {
                        _player = new WasapiOut(AudioClientShareMode.Shared, 0);
                    }
                    else
                    {
                        try
                        {
                            using var enumerator = new MMDeviceEnumerator();
                            var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                                .FirstOrDefault(x => string.Equals(x.ID, deviceId, StringComparison.Ordinal));

                            if (device == null)
                            {
                                _player = new WasapiOut(AudioClientShareMode.Shared, 0);
                            }
                            else
                            {
                                _player = new WasapiOut(device, AudioClientShareMode.Shared, false, 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Audio device fallback failed: {ex.Message}");
                            _player = new WasapiOut(AudioClientShareMode.Shared, 0);
                        }
                    }

                    _player.Init(reader);
                    _player.Play();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Metronome playback failed: {ex.Message}");
                throw;
            }
        }

        private static byte[] CreateToneWave(float frequency, double amplitude, double durationSeconds)
        {
            var sampleRate = 44100;
            var totalSamples = (int)(sampleRate * durationSeconds);
            var blockAlign = 2;
            var bytesPerSecond = sampleRate * blockAlign;
            var totalByteLength = totalSamples * blockAlign;

            var buffer = new byte[44 + totalByteLength];
            using var stream = new MemoryStream(buffer);
            using var writer = new BinaryWriter(stream);

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + totalByteLength);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(bytesPerSecond);
            writer.Write((short)blockAlign);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(totalByteLength);

            var level = Math.Clamp(amplitude, 0.0d, 1.0d);
            var twoPi = 2.0 * Math.PI;

            for (var i = 0; i < totalSamples; i++)
            {
                var value = Math.Sin(twoPi * frequency * i / sampleRate) * level;
                var sample = (short)(value * short.MaxValue);
                writer.Write(sample);
            }

            return buffer;
        }
    }

    public sealed record AudioDeviceInfo(string Id, string Name);
}
