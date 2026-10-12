using System.Globalization;

namespace StellarIndustries.Production
{
    /// <summary>
    /// What a machine is doing, as the host knows it: shown by the panel, and sent to a client
    /// that looks at the machine (clients do not simulate). Encoded as one short text line.
    /// </summary>
    internal struct MachineReport
    {
        public MachineStatus Status;
        /// <summary>The recipe being run (null if none).</summary>
        public string RecipeId;
        /// <summary>The recipe a player chose (null = automatic).</summary>
        public string ChosenId;
        public float Progress;
        public float Watts;
        public float PowerRate;
        /// <summary>Extra line for special machines (deposit of an extractor, fuel of a generator...).</summary>
        public string Detail;

        public string Encode() => string.Join("|",
            ((int)Status).ToString(CultureInfo.InvariantCulture), RecipeId ?? "", ChosenId ?? "",
            Progress.ToString("0.###", CultureInfo.InvariantCulture), Watts.ToString("0.#", CultureInfo.InvariantCulture),
            PowerRate.ToString("0.###", CultureInfo.InvariantCulture), (Detail ?? "").Replace('|', '/'));

        public static bool TryDecode(string text, out MachineReport report)
        {
            report = default;
            if (string.IsNullOrEmpty(text))
                return false;
            string[] p = text.Split('|');
            if (p.Length < 6 || !int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int status))
                return false;
            float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out report.Progress);
            float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out report.Watts);
            float.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out report.PowerRate);
            report.Status = (MachineStatus)status;
            report.RecipeId = p[1].Length > 0 ? p[1] : null;
            report.ChosenId = p[2].Length > 0 ? p[2] : null;
            report.Detail = p.Length > 6 && p[6].Length > 0 ? p[6] : null;
            return true;
        }
    }
}
