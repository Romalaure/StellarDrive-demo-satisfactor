using MelonLoader;

namespace StellarIndustries.Core
{
    /// <summary>Balance and display settings, in UserData/MelonPreferences.cfg, category StellarIndustries.</summary>
    internal static class Settings
    {
        private static MelonPreferences_Category _category;

        public static MelonPreferences_Entry<bool> Enabled { get; private set; }
        public static MelonPreferences_Entry<float> ProductionSpeed { get; private set; }
        public static MelonPreferences_Entry<float> PowerFactor { get; private set; }
        public static MelonPreferences_Entry<bool> RequirePower { get; private set; }
        public static MelonPreferences_Entry<bool> MachineHud { get; private set; }
        public static MelonPreferences_Entry<float> MachineHudDistance { get; private set; }
        public static MelonPreferences_Entry<string> RecipeKey { get; private set; }
        public static MelonPreferences_Entry<string> ScanKey { get; private set; }
        public static MelonPreferences_Entry<bool> ResearchUnlocks { get; private set; }
        public static MelonPreferences_Entry<string> OverviewKey { get; private set; }
        public static MelonPreferences_Entry<float> MachineVolume { get; private set; }
        public static MelonPreferences_Entry<string> Language { get; private set; }
        public static MelonPreferences_Entry<float> ScanRadius { get; private set; }

        public static void Load()
        {
            _category = MelonPreferences.CreateCategory("StellarIndustries", "Stellar Industries");
            Enabled = _category.CreateEntry("Enabled", true,
                description: "Off: the machines stop producing (their parts and contents stay in the world and the saves).");
            ProductionSpeed = _category.CreateEntry("ProductionSpeed", 1f,
                description: "Multiplies every machine's speed (0.1 to 10).");
            PowerFactor = _category.CreateEntry("PowerFactor", 1f,
                description: "Multiplies every machine's power draw (0 to 10).");
            RequirePower = _category.CreateEntry("RequirePower", true,
                description: "Off: machines run at full speed without any power cable (for testing).");
            MachineHud = _category.CreateEntry("MachineHud", true,
                description: "Shows a machine's state, recipe and progress when you look at it.");
            MachineHudDistance = _category.CreateEntry("MachineHudDistance", 4f,
                description: "Distance (meters) up to which a machine you look at shows its state.");
            RecipeKey = _category.CreateEntry("RecipeKey", "N",
                description: "Key that changes the recipe of the machine you look at (Shift + key: previous). A Unity Input System key name: N, B, Y, F6...");
            ScanKey = _category.CreateEntry("ScanKey", "K",
                description: "Key of the deposit scanner: lists the minable rocks around you for a few seconds.");
            ScanRadius = _category.CreateEntry("ScanRadius", 80f,
                description: "Reach of the deposit scanner (meters).");
            ResearchUnlocks = _category.CreateEntry("ResearchUnlocks", true,
                description: "On: the machines are unlocked by the research branch Industrie (research table). Off: all available from the start. Takes effect at the next game start.");
            OverviewKey = _category.CreateEntry("OverviewKey", "F7",
                description: "Key of the factory overview: machines by state, power, problems, simulation cost.");
            MachineVolume = _category.CreateEntry("MachineVolume", 0.5f,
                description: "Volume of the hum of working machines near you (0 = silent, 1 = loud).");
            Language = _category.CreateEntry("Language", "auto",
                description: "Language of the mod: auto (follows the game: French if the game is in French, English otherwise), fr or en.");
            Diagnostics.DevTest.Load(_category);
        }

        public static float Speed => Clamp(ProductionSpeed.Value, 0.1f, 10f);

        public static float Power => Clamp(PowerFactor.Value, 0f, 10f);

        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
