using System;
using System.Collections.Generic;
using System.Globalization;
using Ships.Parts.Common;
using Ships.Parts.Common.Model;
using Ships.Parts.Common.Utils;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// Animates a placed machine while it works: flywheels and drums turn, rams strike, the drill
    /// turns and bores, arms swing, furnace mouths and screens glow brighter, a status light goes
    /// from red to green. Each moving piece is a child named "SI_Anim kind=... axis=x,y,z ..."
    /// (the component is cloned with the part, its own fields are not, so everything is read from
    /// the children). Motion speeds up and slows down smoothly. Purely visual, on every player's
    /// game; whether the machine works comes from <see cref="MachineActivity"/>.
    /// </summary>
    internal sealed class MachineVisuals : DefaultShipPartVisuals, IContextAwarePart
    {
        public const string AnimPrefix = "SI_Anim";
        public const string StatusOn = "SI_StatusOn", StatusOff = "SI_StatusOff";

        private enum Kind { Spin, Bob, Press, Swing, Glow, Slide }

        private sealed class Piece
        {
            public Kind Kind;
            public Transform Transform;
            public Vector3 Axis = Vector3.up;
            public float Speed = 90f, Amplitude, Frequency = 1f, Phase;
            public Vector3 BasePosition;
            public Quaternion BaseRotation;
            public Renderer[] Renderers;
        }

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private readonly List<Piece> _pieces = new List<Piece>();
        private GameObject _on, _off;
        private MaterialPropertyBlock _block;
        private bool _hasContext;
        private uint _ship;
        private ushort _part;
        private float _level, _clock, _nextCheck;
        private bool _active, _settled;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == StatusOn)
                    _on = child.gameObject;
                else if (child.name == StatusOff)
                    _off = child.gameObject;
                else if (child.name.StartsWith(AnimPrefix))
                    Read(child);
            }
            ShowStatus(false);
        }

        private void Read(Transform child)
        {
            var piece = new Piece { Transform = child, BasePosition = child.localPosition, BaseRotation = child.localRotation, Renderers = child.GetComponentsInChildren<Renderer>(true) };
            foreach (string token in child.name.Split(' '))
            {
                string[] pair = token.Split('=');
                if (pair.Length != 2)
                    continue;
                switch (pair[0])
                {
                    case "kind":
                        Enum.TryParse(pair[1], true, out piece.Kind);
                        break;
                    case "axis":
                        string[] xyz = pair[1].Split(',');
                        if (xyz.Length == 3)
                            piece.Axis = new Vector3(F(xyz[0]), F(xyz[1]), F(xyz[2])).normalized;
                        break;
                    case "speed": piece.Speed = F(pair[1]); break;
                    case "amp": piece.Amplitude = F(pair[1]); break;
                    case "freq": piece.Frequency = F(pair[1]); break;
                    case "phase": piece.Phase = F(pair[1]); break;
                }
            }
            _pieces.Add(piece);
        }

        private static float F(string s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

        public void SetContext(ShipPartContext context)
        {
            _hasContext = true;
            _ship = context.Part.ShipId;
            _part = context.Part.PartId;
            _nextCheck = 0f;
        }

        private void Update()
        {
            // Build menu thumbnails, previews and SDModKit's template have no context: they stay still
            if (!_hasContext)
                return;
            if (Time.unscaledTime >= _nextCheck)
            {
                _nextCheck = Time.unscaledTime + 0.25f;
                bool active = false;
                try
                {
                    active = MachineActivity.IsActive(_ship, _part);
                }
                catch (Exception e)
                {
                    ErrorLog.Report("machine animation", e);
                }
                if (active != _active)
                {
                    _active = active;
                    ShowStatus(active);
                }
            }
            // Starts in about a second, runs down in two
            _level = Mathf.MoveTowards(_level, _active ? 1f : 0f, Time.deltaTime * (_active ? 1f : 0.5f));
            // Once stopped, one last pose (glows dimmed), then nothing to do
            if (_level <= 0f && !_active)
            {
                if (_settled)
                    return;
                _settled = true;
            }
            else
            {
                _settled = false;
            }
            _clock += Time.deltaTime * _level;
            foreach (Piece p in _pieces)
                Animate(p);
        }

        private void Animate(Piece p)
        {
            float t = _clock * p.Frequency + p.Phase;
            switch (p.Kind)
            {
                case Kind.Spin:
                    p.Transform.localRotation = p.BaseRotation * Quaternion.AngleAxis(_clock * p.Speed, p.Axis);
                    break;
                case Kind.Bob:
                    p.Transform.localPosition = p.BasePosition + p.Axis * (p.Amplitude * Mathf.Sin(t * Mathf.PI * 2f) * _level);
                    break;
                case Kind.Press:
                {
                    // Quick strike, slow return
                    float c = Mathf.Repeat(t, 1f);
                    float stroke = c < 0.2f ? c / 0.2f : 1f - (c - 0.2f) / 0.8f;
                    p.Transform.localPosition = p.BasePosition - p.Axis * (p.Amplitude * stroke * stroke * _level);
                    break;
                }
                case Kind.Slide:
                    // Along the axis and back to the start, a whole step (amp) at a time: things riding a belt
                    p.Transform.localPosition = p.BasePosition + p.Axis * Mathf.Repeat(_clock * p.Speed, Mathf.Max(0.01f, p.Amplitude));
                    break;
                case Kind.Swing:
                    p.Transform.localRotation = p.BaseRotation * Quaternion.AngleAxis(p.Amplitude * Mathf.Sin(t * Mathf.PI * 2f) * _level, p.Axis);
                    break;
                case Kind.Glow:
                {
                    float flicker = 0.85f + 0.15f * Mathf.Sin(t * 7f) * Mathf.Sin(t * 3.1f);
                    float brightness = Mathf.Lerp(0.25f, 1.6f * flicker, _level);
                    _block.SetColor(EmissionId, new Color(brightness, brightness, brightness));
                    foreach (Renderer r in p.Renderers)
                        r.SetPropertyBlock(_block);
                    break;
                }
            }
        }

        private void ShowStatus(bool active)
        {
            if (_on != null)
                _on.SetActive(active);
            if (_off != null)
                _off.SetActive(!active);
        }
    }
}
