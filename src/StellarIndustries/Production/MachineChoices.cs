using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace StellarIndustries.Production
{
    /// <summary>
    /// The recipe each machine was set to by a player (machines not listed choose by themselves).
    /// The game's save has no room for it (a machine's state is the game's own storage state), so
    /// it is kept in a companion file beside the world save, one line per machine:
    /// "shipId partId recipeId". Unknown recipes in the file are ignored, never an error.
    /// </summary>
    internal sealed class MachineChoices
    {
        public const string Header = "# Stellar Industries: recette choisie par machine (vaisseau pièce recette)";

        private readonly Dictionary<(uint Ship, ushort Part), string> _choices = new Dictionary<(uint, ushort), string>();

        public int Count => _choices.Count;

        public bool Changed { get; set; }

        public string Get(uint ship, ushort part) => _choices.TryGetValue((ship, part), out string id) ? id : null;

        /// <summary>Sets a machine's recipe; null or empty returns it to automatic.</summary>
        public void Set(uint ship, ushort part, string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                if (_choices.Remove((ship, part)))
                    Changed = true;
                return;
            }
            if (Get(ship, part) == recipeId)
                return;
            _choices[(ship, part)] = recipeId;
            Changed = true;
        }

        public void Clear()
        {
            _choices.Clear();
            Changed = false;
        }

        /// <summary>
        /// Forgets the machines that no longer exist. <paramref name="exists"/> answers null when it
        /// cannot tell (ship not loaded): those entries are kept.
        /// </summary>
        public int Prune(Func<uint, ushort, bool?> exists)
        {
            var gone = _choices.Keys.Where(k => exists(k.Ship, k.Part) == false).ToList();
            foreach (var key in gone)
                _choices.Remove(key);
            if (gone.Count > 0)
                Changed = true;
            return gone.Count;
        }

        public IEnumerable<(uint Ship, ushort Part, string Recipe)> All => _choices.Select(c => (c.Key.Ship, c.Key.Part, c.Value));

        public string Serialize()
        {
            var text = new StringBuilder();
            text.Append(Header).Append('\n');
            foreach (var c in _choices.OrderBy(c => c.Key.Ship).ThenBy(c => c.Key.Part))
                text.Append(c.Key.Ship.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(c.Key.Part.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(c.Value).Append('\n');
            return text.ToString();
        }

        /// <summary>Reads a companion file; bad lines are skipped and counted.</summary>
        public int Parse(string text)
        {
            _choices.Clear();
            Changed = false;
            int bad = 0;
            if (string.IsNullOrEmpty(text))
                return 0;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3 || !uint.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out uint ship) ||
                    !ushort.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out ushort part))
                {
                    bad++;
                    continue;
                }
                _choices[(ship, part)] = parts[2];
            }
            return bad;
        }
    }
}
