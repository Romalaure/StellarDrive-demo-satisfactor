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
            Diagnostics.DevTest.Load(_category);
        }

        public static float Speed => Clamp(ProductionSpeed.Value, 0.1f, 10f);

        public static float Power => Clamp(PowerFactor.Value, 0f, 10f);

        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
