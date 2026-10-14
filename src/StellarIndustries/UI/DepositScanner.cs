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
    /// The deposit scanner, an item kept in the player's inventory (the best one counts):
    ///  - Mk1: the minable rocks within 80 m, by ore, the nearest with its distance and a direction arrow;
    ///  - Mk2: twice the reach, a radar screen with every deposit, and their purity;
    ///  - Mk3: almost four times the reach, and markers on the deposits in the player's view;
    ///  - range modules (+25 % each, two at most) and the analysis module (purity and extractor
    ///    yield on any scanner) are kept in the inventory too; a radar tower within 60 m brings
    ///    the reach to 400 m.
    /// One key press scans; the panel stays for a while and follows the player as they walk.
    /// It reads the rocks the player's own game has loaded, so it works the same for every player.
    /// </summary>
    internal static class DepositScanner
    {
        public struct Gear
        {
            public int Tier;
            /// <summary>Range added by the fitted range modules (0.25 = +25 %).</summary>
            public float RangeBonus;
            /// <summary>Best fitted analysis module: 0 none, 1 purity and yield, 2 also markers.</summary>
            public int Analysis;
            public float Range;
            public bool RadarBoost;
            /// <summary>The scanner works without the item (setting ScanNeedsScanner off).</summary>
            public bool Free;
            public bool ShowsPurity => Tier >= 2 || Analysis >= 1;
            public bool ShowsYield => Tier >= 3 || Analysis >= 1;
            public bool Markers => Tier >= 3 || Analysis >= 2;
        }

        private struct Hit
        {
            public uint Item;
            public string Name;
            public Vector3 Position;
            public Purity Purity;
            public bool Node;
        }

        private static readonly List<Hit> Hits = new List<Hit>();
        private static Gear _gear;
        private static Key _key = Key.None;
        private static string _keyName;
        private static float _scannedAt = -100f, _until = -100f;
        private static string _message;
        private static GUIStyle _title, _body, _small, _label;
        private static Texture2D _white, _disc, _dot, _arrow, _diamond, _ring;

        /// <summary>Development: the panel stays until this is cleared (in-game test screenshots).</summary>
        public static bool Hold;

        public static void Forget()
        {
            Hits.Clear();
            _until = -100f;
            _message = null;
        }

        public static void Update()
        {
            if (_keyName != Settings.ScanKey.Value)
            {
                // A second key of its own (K by default; empty or None: only the recipe key)
                _keyName = Settings.ScanKey.Value;
                if (!Enum.TryParse(_keyName?.Trim(), true, out _key))
                    _key = Key.None;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Cursor.visible)
                return;
            // The recipe key scans when no machine is aimed at; with Shift it opens the module bay
            bool recipeKey = keyboard[MachineHud.RecipeKey].wasPressedThisFrame && !MachineHud.Aiming;
            bool ownKey = _key != Key.None && keyboard[_key].wasPressedThisFrame;
            bool shift = keyboard.shiftKey.isPressed;
            // With the scanner in hand: left click scans, right click opens the module bay
            Mouse mouse = Mouse.current;
            bool inHand = InHand();
            bool click = inHand && mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool bay = inHand && mouse != null && mouse.rightButton.wasPressedThisFrame;
            if ((recipeKey || ownKey) && shift || bay)
                ScannerPanel.Toggle();
            else if (recipeKey || ownKey || click)
                Run(null);
        }

        /// <summary>Whether the item the player holds (selected in the toolbar) is a deposit scanner.</summary>
        private static bool InHand()
        {
            var state = PlayerAccess.LocalNetworkState;
            if (state == null)
                return false;
            var selection = state.ItemSelection;
            return selection.HasItemSelected && (selection.ItemId == ResourceCatalog.ScannerMk1 || selection.ItemId == ResourceCatalog.ScannerMk2 || selection.ItemId == ResourceCatalog.ScannerMk3);
        }

        /// <summary>The scanner the player carries now (with its modules and a nearby radar tower).</summary>
        public static Gear CurrentGear()
        {
            Dictionary<uint, int> counts = PlayerAccess.CurrentCounts();
            int Has(uint item) => counts.TryGetValue(item, out int n) ? n : 0;
            var gear = new Gear
            {
                Tier = Has(ResourceCatalog.ScannerMk3) > 0 ? 3 : Has(ResourceCatalog.ScannerMk2) > 0 ? 2 : Has(ResourceCatalog.ScannerMk1) > 0 ? 1 : 0
            };
            if (gear.Tier == 0 && !Settings.ScanNeedsScanner.Value)
            {
                gear.Tier = 1;
                gear.Free = true;
            }
            // The modules fitted in the scanner, as many as it has slots
            IReadOnlyList<uint> fitted = ScannerModules.Own;
            for (int i = 0; i < fitted.Count && i < ScannerModules.Slots(gear.Tier); i++)
            {
                foreach (var kind in ScannerModules.Kinds)
                {
                    if (kind.Item != fitted[i])
                        continue;
                    gear.RangeBonus += kind.Range;
                    gear.Analysis = Mathf.Max(gear.Analysis, kind.Analysis);
                }
            }
            return WithRange(gear);
        }

        private static Gear WithRange(Gear gear)
        {
            float mk1 = Mathf.Clamp(Settings.ScanRadius.Value, 10f, 300f);
            float range = gear.Tier >= 3 ? mk1 * 3.75f : gear.Tier == 2 ? mk1 * 2f : mk1;
            // A radar tower nearby lifts the scanner's own reach; the modules add to either
            float radar = RadarReach();
            if (radar > range)
            {
                range = radar;
                gear.RadarBoost = true;
            }
            range *= 1f + gear.RangeBonus;
            gear.Range = Mathf.Min(range, 800f);
            return gear;
        }

        /// <summary>Scans now with the player's scanner, or with <paramref name="forced"/> (in-game test).</summary>
        private static void Run(Gear? forced)
        {
            _gear = forced ?? CurrentGear();
            _message = null;
            Hits.Clear();
            if (_gear.Tier == 0)
            {
                _message = Lang.T("Il vous faut un <b>scanner de gisements</b> dans l'inventaire.\nFabriquez-le à l'atelier industriel après la recherche « Prospection ».",
                    "You need a <b>deposit scanner</b> in your inventory.\nMake one at the industrial workshop after the \"Prospecting\" research.");
                _scannedAt = Time.unscaledTime;
                _until = Time.unscaledTime + 6f;
                ScannerSound.Refuse();
                return;
            }
            ScannerSound.Scan();
            Vector3 at = PlayerAccess.WorldPosition;
            foreach (Deposits.Rock rock in Deposits.Loaded())
            {
                if (Vector3.Distance(at, rock.Position) <= _gear.Range)
                    Hits.Add(new Hit { Item = rock.Item, Name = IndustrialItems.NameOf(rock.Item), Position = rock.Position, Purity = rock.Purity, Node = rock.Node });
            }
            _scannedAt = Time.unscaledTime;
            _until = Time.unscaledTime + (_gear.Markers ? 30f : _gear.Tier == 2 ? 15f : 10f);
        }

        /// <summary>The scan as text (in-game test): with the player's scanner, or a Mk3 with both modules if <paramref name="best"/>.</summary>
        public static string Scan(bool best = false)
        {
            Run(best ? WithRange(new Gear { Tier = 3, RangeBonus = 1f, Analysis = 2 }) : (Gear?)null);
            if (_message != null)
                return _message;
            var text = new StringBuilder();
            text.Append($"Mk{_gear.Tier} {Mathf.RoundToInt(_gear.Range)} m: {Hits.Count} rock(s)");
            foreach (var group in Hits.GroupBy(h => h.Item))
            {
                text.Append($"\n{IndustrialItems.NameOf(group.Key)}: {group.Count()} ({group.Count(h => h.Purity == Purity.Pure)} pure, " +
                            $"{group.Count(h => h.Purity == Purity.Normal)} normal, {group.Count(h => h.Purity == Purity.Impure)} impure, {group.Count(h => h.Node)} inexhaustible)");
            }
            return text.ToString();
        }

        /// <summary>The reach of the nearest radar tower if the player is within 60 m of one, else 0.</summary>
        private static float RadarReach()
        {
            Camera camera = Camera.main;
            var ships = GameRefs.Service<Ships.Services.ShipsClientTracker>();
            if (camera == null || ships == null)
                return 0f;
            float best = 0f;
            foreach (var ship in ships.VisuallyLoadedShip)
            {
                if (!ships.TryGetShipVisualTransform(ship.Id, out Transform frame) || frame == null || Vector3.Distance(frame.position, camera.transform.position) > 400f)
                    continue;
                foreach (var part in ship.GetAllStatefulParts<Ships.Interface.Model.Parts.State.StorageState>())
                {
                    if (part?.Settings == null || !MachineCatalog.TryGet(part.Settings.id, out MachineDef def) || !def.Radar)
                        continue;
                    if (Vector3.Distance(frame.TransformPoint(part.LocalPosition), camera.transform.position) <= 60f)
                        best = Mathf.Max(best, def.RadarRange);
                }
            }
            return best;
        }

        // ---- Drawing

        private static Color OreColor(uint item)
        {
            switch (item)
            {
                case GameItems.Iron: return new Color(0.85f, 0.5f, 0.38f);
                case GameItems.Copper: return new Color(1f, 0.62f, 0.3f);
                case GameItems.Aluminum: return new Color(0.85f, 0.9f, 1f);
                case GameItems.StellarShard: return new Color(0.75f, 0.55f, 1f);
                case GameItems.Ice: return new Color(0.55f, 0.85f, 1f);
                case GameItems.Glass: return new Color(0.6f, 1f, 0.9f);
                case ResourceCatalog.Coal: return new Color(0.55f, 0.55f, 0.6f);
                case ResourceCatalog.Sulfur: return new Color(1f, 0.92f, 0.3f);
                case ResourceCatalog.Limestone: return new Color(0.92f, 0.88f, 0.75f);
                case ResourceCatalog.RawQuartz: return new Color(1f, 0.8f, 0.95f);
                case ResourceCatalog.GoldOre: return new Color(1f, 0.78f, 0.2f);
                default: return new Color(0.9f, 0.85f, 0.55f);
            }
        }

        /// <summary>Bearing of a point from the camera: degrees, 0 ahead, positive to the right.</summary>
        private static float Bearing(Transform view, Vector3 target, out float flatDistance)
        {
            Vector3 up = view.up;
            Vector3 flat = Vector3.ProjectOnPlane(target - view.position, up);
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, up);
            flatDistance = flat.magnitude;
            if (flat.sqrMagnitude < 0.01f || forward.sqrMagnitude < 0.01f)
                return 0f;
            return Vector3.SignedAngle(forward, flat, up);
        }

        private static string Direction(float angle, float distance)
        {
            if (distance < 2f)
                return Lang.T("juste ici", "right here");
            if (Mathf.Abs(angle) <= 30f)
                return Lang.T("devant", "ahead");
            if (Mathf.Abs(angle) >= 150f)
                return Lang.T("derrière", "behind");
            if (angle > 0f)
                return angle < 90f ? Lang.T("devant à droite", "ahead, right") : Lang.T("derrière à droite", "behind, right");
            return angle > -90f ? Lang.T("devant à gauche", "ahead, left") : Lang.T("derrière à gauche", "behind, left");
        }

        public static void OnGUI()
        {
            if (Time.unscaledTime > _until && !Hold)
                return;
            if (Cursor.visible && !Hold)
                return;
            Styles();
            DrawPulse();
            float fade = Hold ? 1f : Mathf.Clamp01(_until - Time.unscaledTime);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, fade);
            if (_message != null)
                DrawMessage();
            else
            {
                Camera camera = Camera.main;
                if (camera != null && _gear.Markers)
                    DrawMarkers(camera);
                DrawPanel(camera);
            }
            GUI.color = previous;
        }

        /// <summary>The scan's wave: three rings spreading from the middle of the screen and fading, the first second and a half.</summary>
        private static void DrawPulse()
        {
            float age = Time.unscaledTime - _scannedAt;
            if (age < 0f || age > 1.5f)
                return;
            Color tint = _message != null ? new Color(1f, 0.45f, 0.3f) : new Color(0.3f, 0.9f, 1f);
            Color previous = GUI.color;
            var c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            for (int i = 0; i < 3; i++)
            {
                float t = (age - i * 0.18f) / 1.1f;
                if (t <= 0f || t >= 1f)
                    continue;
                float r = Mathf.Lerp(20f, Screen.height * 0.75f, t);
                GUI.color = new Color(tint.r, tint.g, tint.b, (1f - t) * 0.55f);
                GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), _ring);
            }
            GUI.color = previous;
        }

        private static void DrawMessage()
        {
            var content = new GUIContent(_message);
            Vector2 size = _body.CalcSize(content);
            var rect = new Rect(20f, Screen.height * 0.22f, size.x + 24f, size.y + 44f);
            Fill(rect, new Color(0.05f, 0.07f, 0.09f, 0.88f));
            Fill(new Rect(rect.x, rect.y, rect.width, 3f), new Color(1f, 0.55f, 0.2f));
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width, 22f), Lang.T("SCANNER DE GISEMENTS", "DEPOSIT SCANNER"), _title);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 32f, size.x + 4f, size.y), content, _body);
        }

        private static void DrawPanel(Camera camera)
        {
            Transform view = camera != null ? camera.transform : null;
            var groups = Hits.GroupBy(h => h.Item).Select(g => new
            {
                Item = g.Key,
                Hits = g.OrderBy(h => view != null ? Vector3.Distance(view.position, h.Position) : 0f).ToList()
            }).OrderBy(g => view != null ? Vector3.Distance(view.position, g.Hits[0].Position) : 0f).ToList();
            bool radar = _gear.Tier >= 2;
            float width = radar ? 560f : 420f;
            float rows = Mathf.Max(1, groups.Count);
            float listHeight = 34f + rows * (_gear.ShowsPurity ? 44f : 26f);
            float height = 46f + Mathf.Max(listHeight, radar ? 200f : 0f) + 26f;
            var rect = new Rect(20f, Screen.height * 0.18f, width, height);
            Fill(rect, new Color(0.04f, 0.06f, 0.08f, 0.86f));
            Fill(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.3f, 0.85f, 1f));
            // Header: model, reach, modules
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, 300f, 22f), Lang.T("SCANNER DE GISEMENTS", "DEPOSIT SCANNER") + (_gear.Free ? "" : " Mk" + _gear.Tier), _title);
            string reach = Mathf.RoundToInt(_gear.Range) + " m" + (_gear.RadarBoost ? Lang.T(" (tour radar)", " (radar tower)") : "") +
                           (_gear.RangeBonus > 0f ? $"  +{Mathf.RoundToInt(_gear.RangeBonus * 100f)} %" : "") + (_gear.Analysis > 0 ? Lang.T("  analyse ", "  analysis ") + new string('I', _gear.Analysis) : "");
            GUI.Label(new Rect(rect.xMax - 232f, rect.y + 10f, 220f, 20f), "<color=#8cf>" + reach + "</color>", _small);
            // The sweep, the first second
            float sweep = Mathf.Clamp01((Time.unscaledTime - _scannedAt) / 0.9f);
            if (sweep < 1f)
            {
                Fill(new Rect(rect.x + 12f, rect.y + 34f, (rect.width - 24f) * sweep, 3f), new Color(0.3f, 0.85f, 1f, 0.9f));
                GUI.Label(new Rect(rect.x + 12f, rect.y + 40f, 300f, 20f), Lang.T("Balayage...", "Sweeping..."), _small);
                return;
            }
            float listX = rect.x + 12f, y = rect.y + 40f;
            if (groups.Count == 0)
            {
                GUI.Label(new Rect(listX, y, 400f, 22f), Lang.T("Aucun rocher minable à portée.", "No minable rock in range."), _body);
            }
            foreach (var g in groups)
            {
                Hit nearest = g.Hits[0];
                float angle = view != null ? Bearing(view, nearest.Position, out float flat) : 0f;
                float distance = view != null ? Vector3.Distance(view.position, nearest.Position) : 0f;
                Color color = OreColor(g.Item);
                Fill(new Rect(listX, y + 5f, 12f, 12f), color);
                int nodes = g.Hits.Count(h => h.Node);
                GUI.Label(new Rect(listX + 20f, y, 200f, 22f), $"<b>{IndustrialItems.NameOf(g.Item)}</b>  <color=#aaa>×{g.Hits.Count}</color>" +
                    (nodes > 0 ? $"  <color=#c9f>∞{nodes}</color>" : ""), _body);
                // Nearest: distance, arrow, direction words
                Arrow(new Rect(listX + 200f, y + 1f, 20f, 20f), angle, color);
                GUI.Label(new Rect(listX + 226f, y, 200f, 22f), $"{Mathf.RoundToInt(distance)} m <color=#aaa>{Direction(angle, distance)}</color>", _body);
                y += 24f;
                if (_gear.ShowsPurity)
                {
                    int pure = g.Hits.Count(h => h.Purity == Purity.Pure), normal = g.Hits.Count(h => h.Purity == Purity.Normal), impure = g.Hits.Count(h => h.Purity == Purity.Impure);
                    var line = new StringBuilder();
                    line.Append($"<color={Deposits.PurityColor(Purity.Pure)}>{pure} {Lang.T("pur", "pure")}</color> · <color={Deposits.PurityColor(Purity.Normal)}>{normal} normal</color> · <color={Deposits.PurityColor(Purity.Impure)}>{impure} {Lang.T("impur", "impure")}</color>");
                    line.Append(Lang.T("  — le plus proche : ", "  — nearest: ")).Append($"<color={Deposits.PurityColor(nearest.Purity)}>{Deposits.PurityName(nearest.Purity)}</color>");
                    if (_gear.ShowsYield)
                    {
                        MachineCatalog.TryGet(MachineCatalog.Extractor, out MachineDef extractor);
                        float perMinute = 60f / (extractor?.ExtractSeconds ?? 6f) * DepositPurity.Factor(nearest.Purity) * Settings.Speed;
                        line.Append(Lang.T($", {perMinute:0}/min", $", {perMinute:0}/min"));
                    }
                    GUI.Label(new Rect(listX + 20f, y - 2f, 400f, 20f), line.ToString(), _small);
                    y += 20f;
                }
            }
            if (radar && view != null)
                DrawRadar(new Rect(rect.xMax - 200f, rect.y + 40f, 186f, 186f), view);
            string hint = Lang.T($"[{MachineHud.RecipeKey}] nouveau balayage — un extracteur doit être à moins de 6 m d'un rocher", $"[{MachineHud.RecipeKey}] scan again — an extractor must be within 6 m of a rock");
            if (_gear.ShowsPurity)
                hint += Lang.T(" (pur ×2, impur ×½, ∞ inépuisable ×2,5)", " (pure ×2, impure ×½, ∞ inexhaustible ×2.5)");
            GUI.Label(new Rect(rect.x + 12f, rect.yMax - 22f, rect.width - 20f, 20f), "<color=#9aa>" + hint + "</color>", _small);
        }

        /// <summary>Radar screen: the player in the middle facing up, each deposit a dot (bigger when pure), a sweeping beam.</summary>
        private static void DrawRadar(Rect r, Transform view)
        {
            GUI.DrawTexture(r, _disc);
            Vector2 c = r.center;
            float radius = r.width * 0.5f - 6f;
            // The screen's scale: the farthest deposit found (rounded up to 10 m), not the whole reach
            float scale = 30f;
            foreach (Hit h in Hits)
            {
                Bearing(view, h.Position, out float far);
                scale = Mathf.Max(scale, far * 1.1f);
            }
            scale = Mathf.Min(Mathf.Ceil(scale / 10f) * 10f, _gear.Range);
            foreach (Hit h in Hits)
            {
                float angle = Bearing(view, h.Position, out float flat);
                float d = Mathf.Min(1f, flat / scale) * radius;
                float rad = angle * Mathf.Deg2Rad;
                var p = new Vector2(c.x + Mathf.Sin(rad) * d, c.y - Mathf.Cos(rad) * d);
                float size = h.Purity == Purity.Pure ? 9f : h.Purity == Purity.Normal ? 7f : 5f;
                Color previous = GUI.color;
                GUI.color = OreColor(h.Item) * new Color(1f, 1f, 1f, previous.a);
                GUI.DrawTexture(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), _dot);
                GUI.color = previous;
            }
            // The player: a small arrow pointing ahead
            Arrow(new Rect(c.x - 7f, c.y - 7f, 14f, 14f), 0f, Color.white);
            // Beam turning once every two seconds
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Time.unscaledTime * 180f % 360f, c);
            Color before = GUI.color;
            GUI.color = new Color(0.3f, 1f, 0.6f, 0.35f * before.a);
            GUI.DrawTexture(new Rect(c.x - 1f, c.y - radius, 2f, radius), _white);
            GUI.color = before;
            GUI.matrix = matrix;
            GUI.Label(new Rect(r.x, r.yMax - 16f, r.width, 16f), "<color=#7c9>" + Lang.T("bord : ", "edge: ") + Mathf.RoundToInt(scale) + " m</color>", _small);
        }

        /// <summary>Markers on the deposits in view (Mk3): ore color, purity, distance.</summary>
        private static void DrawMarkers(Camera camera)
        {
            Vector3 from = camera.transform.position;
            foreach (Hit h in Hits.OrderBy(x => Vector3.Distance(from, x.Position)).Take(40))
            {
                Vector3 screen = camera.WorldToScreenPoint(h.Position + camera.transform.up * 1.2f);
                if (screen.z <= 0f)
                    continue;
                float x = screen.x, y = Screen.height - screen.y;
                if (x < -50f || x > Screen.width + 50f || y < -50f || y > Screen.height + 50f)
                    continue;
                float size = h.Purity == Purity.Pure ? 18f : h.Purity == Purity.Normal ? 14f : 11f;
                Color previous = GUI.color;
                GUI.color = OreColor(h.Item) * new Color(1f, 1f, 1f, previous.a);
                GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), _diamond);
                GUI.color = previous;
                string text = $"{h.Name} <color={Deposits.PurityColor(h.Purity)}>{Deposits.PurityName(h.Purity)}</color>" +
                              (h.Node ? Lang.T(" <color=#c9f>∞ inépuisable</color>", " <color=#c9f>∞ inexhaustible</color>") : "") + $"\n{Mathf.RoundToInt(Vector3.Distance(from, h.Position))} m";
                GUI.Label(new Rect(x - 80f, y + size * 0.5f + 1f, 160f, 34f), text, _label);
            }
        }

        private static void Arrow(Rect r, float angle, Color color)
        {
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, r.center);
            Color previous = GUI.color;
            GUI.color = color * new Color(1f, 1f, 1f, previous.a);
            GUI.DrawTexture(r, _arrow);
            GUI.color = previous;
            GUI.matrix = matrix;
        }

        private static void Fill(Rect r, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color * new Color(1f, 1f, 1f, previous.a);
            GUI.DrawTexture(r, _white);
            GUI.color = previous;
        }

        private static void Styles()
        {
            if (_title != null)
                return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = new Color(0.85f, 0.95f, 1f) } };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = false, normal = { textColor = Color.white } };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, wordWrap = false, normal = { textColor = new Color(0.85f, 0.88f, 0.9f) } };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, alignment = TextAnchor.UpperCenter, normal = { textColor = Color.white } };
            _white = Texture(4, (x, y) => Color.white);
            _dot = Texture(16, (x, y) => Disc(x, y, 16, 7.5f) ? Color.white : Clear);
            _ring = Texture(256, (x, y) =>
            {
                float d = Mathf.Sqrt((x - 127.5f) * (x - 127.5f) + (y - 127.5f) * (y - 127.5f));
                float edge = Mathf.Abs(d - 124f);
                return edge < 3f ? new Color(1f, 1f, 1f, 1f - edge / 3f) : d < 124f ? new Color(1f, 1f, 1f, 0.05f * d / 124f) : Clear;
            });
            _diamond = Texture(32, (x, y) =>
            {
                float d = Mathf.Abs(x - 15.5f) + Mathf.Abs(y - 15.5f);
                return d < 13f ? (d > 10f ? Color.white : new Color(1f, 1f, 1f, 0.7f)) : Clear;
            });
            // Pointing up (row 0 is the bottom of the texture)
            _arrow = Texture(32, (x, y) =>
            {
                float u = x - 15.5f;
                bool head = y > 16 && Mathf.Abs(u) < (31 - y) * 0.9f;
                bool shaft = y <= 16 && y > 3 && Mathf.Abs(u) < 3.5f;
                return head || shaft ? Color.white : Clear;
            });
            _disc = Texture(186, (x, y) =>
            {
                float dx = x - 92.5f, dy = y - 92.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 92f)
                    return Clear;
                bool ring = Mathf.Abs(r - 89f) < 1.5f || Mathf.Abs(r - 60f) < 0.7f || Mathf.Abs(r - 30f) < 0.7f;
                bool cross = Mathf.Abs(dx) < 0.6f || Mathf.Abs(dy) < 0.6f;
                return ring ? new Color(0.3f, 0.9f, 0.6f, 0.8f) : cross ? new Color(0.3f, 0.9f, 0.6f, 0.25f) : new Color(0.02f, 0.12f, 0.08f, 0.85f);
            });
        }

        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        private static bool Disc(int x, int y, int size, float radius)
        {
            float c = (size - 1) * 0.5f;
            return (x - c) * (x - c) + (y - c) * (y - c) <= radius * radius;
        }

        private static Texture2D Texture(int size, Func<int, int, Color> pixel)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    t.SetPixel(x, y, pixel(x, y));
            }
            t.Apply();
            return t;
        }
    }
}
