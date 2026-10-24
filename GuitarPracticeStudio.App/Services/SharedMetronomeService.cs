using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RiffLab.App.Services
{
    public static class SharedMetronomeService
    {
        private static readonly DispatcherTimer Timer = new();
        private static int _bpm = 120;
        private static double _volume = 0.6;

        static SharedMetronomeService()
        {
            Timer.Interval = TimeSpan.FromMilliseconds(60000.0 / _bpm);
            Timer.Tick += (_, _) =>
            {
                if (!IsRunning)
                {
                    return;
                }

                var accent = CurrentBeatIndex == 0;
                CurrentBeatIndex = (CurrentBeatIndex + 1) % BeatsPerMeasure;
                _ = Task.Run(() => AudioOutputService.Instance.PlayMetronomeClick(accent, Volume));
                StateChanged?.Invoke();
            };
        }

        public static int Bpm
        {
            get => _bpm;
            set
            {
                _bpm = Math.Clamp(value, 40, 240);
                Timer.Interval = TimeSpan.FromMilliseconds(60000.0 / _bpm);
                StateChanged?.Invoke();
            }
        }

        public static double Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0.0, 1.0);
                StateChanged?.Invoke();
            }
        }

        public static int BeatsPerMeasure { get; set; } = 4;
        public static bool IsRunning { get; private set; }
        public static int CurrentBeatIndex { get; private set; }
        public static event Action? StateChanged;

        public static void Start()
        {
            if (IsRunning)
            {
                return;
            }

            IsRunning = true;
            CurrentBeatIndex = 1;
            Timer.Interval = TimeSpan.FromMilliseconds(60000.0 / Bpm);
            Timer.Start();
            _ = Task.Run(() => AudioOutputService.Instance.PlayMetronomeClick(true, Volume));
            StateChanged?.Invoke();
        }

        public static void Stop()
        {
            IsRunning = false;
            Timer.Stop();
            CurrentBeatIndex = 0;
            AudioOutputService.Instance.Stop();
            StateChanged?.Invoke();
        }

        public static void SetBpm(int value)
        {
            Bpm = value;
            if (IsRunning)
            {
                Timer.Stop();
                Timer.Interval = TimeSpan.FromMilliseconds(60000.0 / Bpm);
                Timer.Start();
            }
        }

        public static void SetVolume(double value)
        {
            Volume = value;
        }

        public static void SetBeatsPerMeasure(int value)
        {
            BeatsPerMeasure = Math.Clamp(value, 2, 6);
            CurrentBeatIndex = 0;
            StateChanged?.Invoke();
        }
    }
}
