using System;
using System.IO;
using FishNet;
using RuntimeManager = FMODUnity.RuntimeManager;
using Settings = StellarIndustries.Core.Settings;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using Ships.Services;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using UnityEngine;

namespace StellarIndustries.UI
{
    /// <summary>
    /// The hum of working machines around the player. The game's Unity audio is off, every sound
    /// goes through FMOD: one looping 2D channel whose volume follows the distance to the nearest
    /// working machine. The sound itself is made by the mod (a low motor hum with a slow mechanical
    /// beat, synthesized into a WAV file on first use), no recorded or borrowed audio.
    /// The host knows which machines work; a client counts a machine as working when its slots
    /// hold what a recipe needs (it does not know the power).
    /// </summary>
    internal static class MachineSound
    {
        private const float Reach = 15f;
        private const int Rate = 22050;

        private static FMOD.Sound _sound;
        private static FMOD.Channel _channel;
        private static bool _started, _failed;
        private static float _target, _volume, _nextLook;
        private static ShipsClientTracker _ships;
        private static readonly Stack[] Slots = new Stack[10];

        public static float CurrentVolume => _volume;

        public static void Update()
        {
            if (_failed)
                return;
            float setting = Mathf.Clamp01(Settings.MachineVolume.Value);
            if (Time.unscaledTime >= _nextLook)
            {
                _nextLook = Time.unscaledTime + 0.25f;
                _target = setting > 0f ? setting * Loudness() : 0f;
            }
            _volume = Mathf.MoveTowards(_volume, _target, Time.unscaledDeltaTime * 0.8f);
            if (_volume <= 0.001f && !_started)
                return;
            if (!_started && !Start())
                return;
            _channel.setVolume(_volume);
            _channel.setPaused(_volume <= 0.001f);
        }

        /// <summary>0 (no working machine within reach) to 1 (one right here).</summary>
        private static float Loudness()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return 0f;
            if (_ships == null)
                _ships = SDModKit.Game.GameRefs.Service<ShipsClientTracker>();
            if (_ships == null)
                return 0f;
            Vector3 ear = camera.transform.position;
            float nearest = Reach;
            bool host = SINet.IsServer;
            foreach (var ship in _ships.VisuallyLoadedShip)
            {
                if (!_ships.TryGetShipVisualTransform(ship.Id, out Transform frame) || frame == null)
                    continue;
                // Whole ship far away: skip its parts
                if (Vector3.Distance(frame.position, ear) > Reach + 60f)
                    continue;
                foreach (StatefulPart part in ship.GetAllStatefulParts<StorageState>())
                {
                    if (part?.Settings == null || !MachineCatalog.TryGet(part.Settings.id, out MachineDef def) || def.IsLogistics)
                        continue;
                    float d = Vector3.Distance(frame.TransformPoint(part.LocalPosition), ear);
                    if (d >= nearest || !Working(host, ship.Id, part, def))
                        continue;
                    nearest = d;
                }
            }
            float t = 1f - nearest / Reach;
            return t * t;
        }

        private static bool Working(bool host, uint shipId, StatefulPart part, MachineDef def)
        {
            if (host)
            {
                MachineSimulation.Runtime rt = MachineSimulation.Get(shipId, part.Id);
                return rt != null && (rt.Status == MachineStatus.Producing || rt.Status == MachineStatus.Underpowered);
            }
            MachineReport? report = MachineControl.ClientReport(shipId, part.Id, 3f);
            if (report != null)
                return report.Value.Status == MachineStatus.Producing || report.Value.Status == MachineStatus.Underpowered;
            if (def.Extracts || def.Generates || !(part.State is StorageState state))
                return false;
            MachineSimulation.Read(state, Slots);
            return RecipeSelector.Pick(Slots, RecipeBook.ForMachine(def.PartId), null, IndustrialItems.MaxStack, out _) != null;
        }

        private static bool Start()
        {
            try
            {
                string path = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "StellarIndustries", "sounds", "machine_hum.wav");
                byte[] wav = Synthesize();
                if (!File.Exists(path) || new FileInfo(path).Length != wav.Length)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, wav);
                }
                FMOD.System core = RuntimeManager.CoreSystem;
                if (core.createSound(path, FMOD.MODE.LOOP_NORMAL | FMOD.MODE._2D, out _sound) != FMOD.RESULT.OK)
                    return Fail("FMOD could not load the machine sound");
                core.getMasterChannelGroup(out FMOD.ChannelGroup master);
                if (core.playSound(_sound, master, true, out _channel) != FMOD.RESULT.OK)
                    return Fail("FMOD could not play the machine sound");
                _channel.setVolume(0f);
                _started = true;
                return true;
            }
            catch (Exception e)
            {
                return Fail("machine sound: " + e.Message);
            }
        }

        private static bool Fail(string message)
        {
            _failed = true;
            IndustriesMod.Log.Warning(message + " (machines stay silent)");
            return false;
        }

        public static void Stop()
        {
            if (!_started)
                return;
            _channel.stop();
            _sound.release();
            _started = false;
            _volume = 0f;
        }

        /// <summary>
        /// Two seconds that loop seamlessly: a 55 Hz motor with its harmonics, a soft belt whine
        /// and a slow thump twice a second (every frequency fits a whole number of times in 2 s).
        /// </summary>
        public static byte[] Synthesize()
        {
            const int seconds = 2;
            int samples = Rate * seconds;
            var data = new short[samples];
            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / Rate;
                double v = 0.42 * Math.Sin(2 * Math.PI * 55 * t)
                           + 0.22 * Math.Sin(2 * Math.PI * 110 * t + 0.6)
                           + 0.12 * Math.Sin(2 * Math.PI * 165 * t + 1.1)
                           + 0.05 * Math.Sin(2 * Math.PI * 880 * t) * (0.6 + 0.4 * Math.Sin(2 * Math.PI * 3 * t));
                // Thump: a decaying 70 Hz knock every half second
                double phase = t % 0.5;
                v += 0.35 * Math.Exp(-phase * 28) * Math.Sin(2 * Math.PI * 70 * phase);
                v *= 0.8 + 0.2 * Math.Sin(2 * Math.PI * 0.5 * t);
                data[i] = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, v * 0.55 * short.MaxValue));
            }
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + samples * 2);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(Rate);
                w.Write(Rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(samples * 2);
                foreach (short s in data)
                    w.Write(s);
                w.Flush();
                return stream.ToArray();
            }
        }
    }
}
