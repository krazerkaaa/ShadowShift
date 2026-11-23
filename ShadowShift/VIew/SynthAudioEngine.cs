using System.Media;
using System.Runtime.InteropServices;
using System.Text;

namespace ShadowShift.View
{
    // Процедурный аудиосинтезатор: генерирует фоновый кибер-бит и звуковые эффекты в памяти
    public sealed class SynthAudioEngine : IDisposable
    {
        private const int SampleRate = 44100;

        private readonly SoundPlayer _jumpSound;
        private readonly SoundPlayer _phaseSound;
        private readonly SoundPlayer _crashSound;
        private readonly SoundPlayer _winSound;
        private readonly SoundPlayer _menuSound;
        private readonly SoundPlayer _speedUpSound;
        private readonly SoundPlayer _slowDownSound;
        private readonly string _beatFilePath;

        public bool IsMuted { get; private set; }

        // Системная функция Windows для независимого зацикливания фоновой музыки
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern long mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr winHandle);

        public SynthAudioEngine()
        {
            // Генерируем звуковые эффекты в оперативной памяти
            _jumpSound = CreatePlayer(GenerateSweep(190, 560, 0.13, 0.35, squareWave: true));
            _phaseSound = CreatePlayer(GenerateSweep(780, 290, 0.10, 0.30, squareWave: false));
            _crashSound = CreatePlayer(GenerateNoise(0.24, 0.45));
            _winSound = CreatePlayer(GenerateArpeggio(new[] { 440.0, 554.37, 659.25, 880.0 }, 0.11, 0.35));
            _menuSound = CreatePlayer(GenerateSweep(420, 520, 0.05, 0.20, squareWave: true));
            _speedUpSound = CreatePlayer(GenerateSweep(300, 980, 0.18, 0.32, squareWave: false));
            _slowDownSound = CreatePlayer(GenerateSweep(720, 140, 0.22, 0.32, squareWave: false));

            // Сохраняем сгенерированный фоновый луп во временный WAV-файл для MCI-канала
            var tempDir = Path.Combine(Path.GetTempPath(), "ShadowShiftAudio");
            Directory.CreateDirectory(tempDir);
            _beatFilePath = Path.Combine(tempDir, "cyber_beat.wav");
            File.WriteAllBytes(_beatFilePath, GenerateCyberBeatLoop());

            mciSendString($"open \"{_beatFilePath}\" type mpegvideo alias bgm", null, 0, IntPtr.Zero);
            mciSendString("setaudio bgm volume to 450", null, 0, IntPtr.Zero);
        }

        public void StartBackgroundBeat()
        {
            if (IsMuted) return;
            mciSendString("play bgm from 0 repeat", null, 0, IntPtr.Zero);
        }

        public void StopBackgroundBeat()
        {
            mciSendString("stop bgm", null, 0, IntPtr.Zero);
        }

        // Включает или выключает звук по нажатию клавиши M
        public void ToggleMute(bool isPlayingNow)
        {
            IsMuted = !IsMuted;
            if (IsMuted)
                StopBackgroundBeat();
            else if (isPlayingNow)
                StartBackgroundBeat();
        }

        public void PlayJump() => PlaySfx(_jumpSound);
        public void PlayPhaseSwitch() => PlaySfx(_phaseSound);
        public void PlayCrash() => PlaySfx(_crashSound);
        public void PlayWin() => PlaySfx(_winSound);
        public void PlayMenuTick() => PlaySfx(_menuSound);

        public void PlaySpeedChange(float multiplier)
        {
            if (multiplier > 1.01f)
                PlaySfx(_speedUpSound);
            else
                PlaySfx(_slowDownSound);
        }

        private void PlaySfx(SoundPlayer player)
        {
            if (!IsMuted)
                player.Play();
        }

        private static SoundPlayer CreatePlayer(byte[] wavBytes)
        {
            var stream = new MemoryStream(wavBytes);
            var player = new SoundPlayer(stream);
            player.Load();
            return player;
        }

        // Синтезирует скользящий по частоте сигнал (для прыжка, фазы и порталов)
        private static byte[] GenerateSweep(double startFreq, double endFreq, double durationSec, double volume, bool squareWave)
        {
            var sampleCount = (int)(SampleRate * durationSec);
            var samples = new short[sampleCount];
            var phase = 0.0;

            for (var i = 0; i < sampleCount; i++)
            {
                var progress = (double)i / sampleCount;
                var freq = startFreq + (endFreq - startFreq) * progress;
                var envelope = 1.0 - progress;

                phase += 2.0 * Math.PI * freq / SampleRate;
                var wave = squareWave ? Math.Sign(Math.Sin(phase)) : Math.Sin(phase);
                samples[i] = (short)(wave * envelope * volume * short.MaxValue);
            }

            return BuildWavBytes(samples);
        }

        // Синтезирует цифровой шум с затуханием (для столкновения)
        private static byte[] GenerateNoise(double durationSec, double volume)
        {
            var sampleCount = (int)(SampleRate * durationSec);
            var samples = new short[sampleCount];
            var rng = new Random(42);

            for (var i = 0; i < sampleCount; i++)
            {
                var envelope = 1.0 - (double)i / sampleCount;
                var noise = rng.NextDouble() * 2.0 - 1.0;
                samples[i] = (short)(noise * envelope * envelope * volume * short.MaxValue);
            }

            return BuildWavBytes(samples);
        }

        // Синтезирует восходящий аккорд при победе
        private static byte[] GenerateArpeggio(double[] notes, double noteDurationSec, double volume)
        {
            var samplesPerNote = (int)(SampleRate * noteDurationSec);
            var samples = new short[samplesPerNote * notes.Length];

            for (var n = 0; n < notes.Length; n++)
            {
                var phase = 0.0;
                for (var i = 0; i < samplesPerNote; i++)
                {
                    var envelope = 1.0 - (double)i / samplesPerNote * 0.7;
                    phase += 2.0 * Math.PI * notes[n] / SampleRate;
                    var wave = Math.Sign(Math.Sin(phase)) * 0.5 + Math.Sin(phase) * 0.5;
                    samples[n * samplesPerNote + i] = (short)(wave * envelope * volume * short.MaxValue);
                }
            }

            return BuildWavBytes(samples);
        }

        // Синтезирует зацикленный киберпанк-бит (бочка + басовая линия, 130 BPM)
        private static byte[] GenerateCyberBeatLoop()
        {
            const double bpm = 130.0;
            const double stepDuration = 60.0 / bpm / 2.0;
            const int totalSteps = 16;

            double[] bassNotes =
            {
                55.0, 55.0, 65.41, 55.0, 73.42, 73.42, 65.41, 82.41,
                55.0, 55.0, 65.41, 55.0, 82.41, 73.42, 65.41, 49.00
            };

            var samplesPerStep = (int)(SampleRate * stepDuration);
            var samples = new short[samplesPerStep * totalSteps];
            var bassPhase = 0.0;

            for (var step = 0; step < totalSteps; step++)
            {
                var hasKick = step % 2 == 0;
                var kickPhase = 0.0;

                for (var i = 0; i < samplesPerStep; i++)
                {
                    var stepProgress = (double)i / samplesPerStep;
                    var mix = 0.0;

                    if (hasKick && stepProgress < 0.45)
                    {
                        var kickEnv = 1.0 - stepProgress / 0.45;
                        var kickFreq = 135.0 * Math.Pow(0.25, stepProgress / 0.45);
                        kickPhase += 2.0 * Math.PI * kickFreq / SampleRate;
                        mix += Math.Sin(kickPhase) * kickEnv * kickEnv * 0.55;
                    }

                    var bassEnv = 1.0 - stepProgress * 0.55;
                    bassPhase += 2.0 * Math.PI * bassNotes[step] / SampleRate;
                    var sawWave = (bassPhase % (2.0 * Math.PI)) / Math.PI - 1.0;
                    mix += sawWave * bassEnv * 0.22;

                    mix = Math.Clamp(mix, -1.0, 1.0);
                    samples[step * samplesPerStep + i] = (short)(mix * short.MaxValue);
                }
            }

            return BuildWavBytes(samples);
        }

        // Упаковывает массив сэмплов в стандартный 16-битный WAV (RIFF PCM)
        private static byte[] BuildWavBytes(short[] samples)
        {
            var dataSize = samples.Length * 2;
            using var ms = new MemoryStream(44 + dataSize);
            using var writer = new BinaryWriter(ms, Encoding.ASCII);

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (var sample in samples)
                writer.Write(sample);

            return ms.ToArray();
        }

        public void Dispose()
        {
            mciSendString("close bgm", null, 0, IntPtr.Zero);
            _jumpSound.Dispose();
            _phaseSound.Dispose();
            _crashSound.Dispose();
            _winSound.Dispose();
            _menuSound.Dispose();
            _speedUpSound.Dispose();
            _slowDownSound.Dispose();
        }
    }
}