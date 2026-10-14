using System;
using System.IO;
using RuntimeManager = FMODUnity.RuntimeManager;
using Settings = StellarIndustries.Core.Settings;
using UnityEngine;

namespace StellarIndustries.UI
{
    /// <summary>
    /// The scanner's sounds, made by the mod (no recorded audio) and played through FMOD like the
    /// machines' hum: a rising sweep with two pings when a scan starts, a short low buzz when it
    /// cannot run (no scanner). Volume: setting ScanVolume.
    /// </summary>
    internal static class ScannerSound
    {
        private const int Rate = 22050;

        private static FMOD.Sound _scan, _refuse;
        private static bool _ready, _failed;

        public static void Scan() => Play(true);

        public static void Refuse() => Play(false);

        private static void Play(bool scan)
        {
            float volume = Mathf.Clamp01(Settings.ScanVolume.Value);
            if (volume <= 0f || _failed || (!_ready && !Load()))
                return;
            try
            {
                FMOD.System core = RuntimeManager.CoreSystem;
                core.getMasterChannelGroup(out FMOD.ChannelGroup master);
                if (core.playSound(scan ? _scan : _refuse, master, true, out FMOD.Channel channel) == FMOD.RESULT.OK)
                {
                    channel.setVolume(volume);
                    channel.setPaused(false);
                }
            }
            catch (Exception e)
            {
                _failed = true;
                IndustriesMod.Log.Warning("scanner sound: " + e.Message);
            }
        }

        private static bool Load()
        {
            try
            {
                string folder = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "StellarIndustries", "sounds");
                Directory.CreateDirectory(folder);
                FMOD.System core = RuntimeManager.CoreSystem;
                string scan = Path.Combine(folder, "scanner_sweep.wav"), refuse = Path.Combine(folder, "scanner_refuse.wav");
                File.WriteAllBytes(scan, Wav(Sweep()));
                File.WriteAllBytes(refuse, Wav(Buzz()));
                if (core.createSound(scan, FMOD.MODE.DEFAULT | FMOD.MODE._2D, out _scan) != FMOD.RESULT.OK ||
                    core.createSound(refuse, FMOD.MODE.DEFAULT | FMOD.MODE._2D, out _refuse) != FMOD.RESULT.OK)
                {
                    _failed = true;
                    IndustriesMod.Log.Warning("scanner sound: FMOD could not load it (the scanner stays silent)");
                    return false;
                }
                _ready = true;
                return true;
            }
            catch (Exception e)
            {
                _failed = true;
                IndustriesMod.Log.Warning("scanner sound: " + e.Message);
                return false;
            }
        }

        /// <summary>1.3 s: a chirp rising from 300 to 1500 Hz with a shimmer, then two pings at 1760 Hz.</summary>
        public static short[] Sweep()
        {
            int samples = (int)(Rate * 1.3f);
            var data = new short[samples];
            double phase = 0;
            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / Rate;
                double v = 0;
                if (t < 0.8)
                {
                    double f = 300 + 1200 * (t / 0.8) * (t / 0.8);
                    phase += 2 * Math.PI * f / Rate;
                    double envelope = Math.Min(1, t * 20) * (1 - t / 0.8 * 0.4);
                    v = envelope * (0.5 * Math.Sin(phase) + 0.15 * Math.Sin(phase * 2.01)) * (0.8 + 0.2 * Math.Sin(2 * Math.PI * 18 * t));
                }
                foreach (double start in new[] { 0.82, 1.02 })
                {
                    double p = t - start;
                    if (p >= 0)
                        v += 0.45 * Math.Exp(-p * 18) * Math.Sin(2 * Math.PI * 1760 * p) + 0.12 * Math.Exp(-p * 25) * Math.Sin(2 * Math.PI * 3520 * p);
                }
                data[i] = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, v * 0.6 * short.MaxValue));
            }
            return data;
        }

        /// <summary>0.3 s: two short low buzzes.</summary>
        public static short[] Buzz()
        {
            int samples = (int)(Rate * 0.3f);
            var data = new short[samples];
            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / Rate;
                double gate = t % 0.15 < 0.1 ? 1 : 0;
                double v = gate * (0.5 * Math.Sign(Math.Sin(2 * Math.PI * 140 * t)) * 0.4 + 0.3 * Math.Sin(2 * Math.PI * 280 * t));
                data[i] = (short)(v * 0.5 * short.MaxValue);
            }
            return data;
        }

        private static byte[] Wav(short[] data)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + data.Length * 2);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(Rate);
                w.Write(Rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(data.Length * 2);
                foreach (short s in data)
                    w.Write(s);
                w.Flush();
                return stream.ToArray();
            }
        }
    }
}
