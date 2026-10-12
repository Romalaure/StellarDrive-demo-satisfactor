using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SDModKit.Game;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StellarIndustries.UI
{
    /// <summary>
    /// The deposit scanner: one key press lists, for eight seconds, the nearest minable rocks of
    /// each kind around the player (distance and direction), to choose where to set an extractor.
    /// It reads the rocks the player's own game has loaded, so it works the same for every player.
    /// </summary>
    internal static class DepositScanner
    {
        private const float ShowSeconds = 8f;

        private static Key _key = Key.None;
        private static string _keyName;
        private static string _shown;
        private static float _until;
        private static GUIStyle _style;

        public static void Update()
        {
            if (_key == Key.None || _keyName != Settings.ScanKey.Value)
            {
                _keyName = Settings.ScanKey.Value;
                if (!Enum.TryParse(_keyName?.Trim(), true, out _key) || _key == Key.None)
                    _key = Key.K;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && !Cursor.visible && keyboard[_key].wasPressedThisFrame)
            {
                _shown = Scan();
                _until = Time.unscaledTime + ShowSeconds;
            }
            if (_shown != null && Time.unscaledTime > _until)
                _shown = null;
        }

        /// <summary>The scan text (also used by the in-game test).</summary>
        public static string Scan()
        {
            float radius = Mathf.Clamp(Settings.ScanRadius.Value, 10f, 300f);
            List<MinableHit> hits = WorldQuery.ScanMinables(radius, null).Where(h => Deposits.Extractable(h.ItemId)).ToList();
            var text = new StringBuilder();
            text.Append("<b>Scanner de gisements</b> (").Append(Mathf.RoundToInt(radius)).Append(" m)");
            if (hits.Count == 0)
            {
                text.Append("\nAucun rocher minable à portée.");
                return text.ToString();
            }
            Camera camera = Camera.main;
            foreach (var group in hits.GroupBy(h => h.ItemId).OrderBy(g => g.Min(h => h.Distance)))
            {
                MinableHit nearest = group.OrderBy(h => h.Distance).First();
                text.Append("\n").Append(IndustrialItems.NameOf(group.Key)).Append(" : ").Append(group.Count()).Append(" rocher(s), le plus proche à ")
                    .Append(Mathf.RoundToInt(nearest.Distance)).Append(" m");
                if (camera != null)
                    text.Append(", ").Append(Direction(camera.transform, nearest.Position));
            }
            text.Append("\n<color=#aaa>Un extracteur doit être à moins de 6 m d'un rocher.</color>");
            return text.ToString();
        }

        private static string Direction(Transform view, Vector3 target)
        {
            Vector3 to = target - view.position;
            Vector3 up = view.up;
            Vector3 flat = Vector3.ProjectOnPlane(to, up);
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, up);
            if (flat.sqrMagnitude < 0.01f || forward.sqrMagnitude < 0.01f)
                return "juste ici";
            float angle = Vector3.SignedAngle(forward, flat, up);
            if (Mathf.Abs(angle) <= 30f)
                return "devant";
            if (Mathf.Abs(angle) >= 150f)
                return "derrière";
            if (angle > 0f)
                return angle < 90f ? "devant à droite" : "derrière à droite";
            return angle > -90f ? "devant à gauche" : "derrière à gauche";
        }

        public static void OnGUI()
        {
            if (_shown == null)
                return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, fontSize = 15, wordWrap = false, padding = new RectOffset(10, 10, 8, 8) };
            var content = new GUIContent(_shown);
            Vector2 size = _style.CalcSize(content);
            GUI.Box(new Rect(20f, Screen.height * 0.25f, size.x, size.y), content, _style);
        }
    }
}
