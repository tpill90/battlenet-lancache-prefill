namespace BattleNetPrefill
{
    //TODO this needs to migrate away from EnumBase
    /// <summary>
    /// A list of all possible games that can be downloaded from Battle.Net.  This list is not comprehensive, as it does not include alpha/beta/PTR versions,
    /// however it does include all games that are currently playable today.
    ///
    /// A complete list can be found at : https://blizztrack.com/
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public sealed class TactProduct : EnumBase<TactProduct>
    {
        #region Blizzard

        public static readonly TactProduct BlizzardArcadeCollection = new TactProduct("rtro") { DisplayName = "Blizzard Arcade Collection", DefaultTags = new[] { "dummy" }, IsBlizzard = true };

        public static readonly TactProduct DiabloImmortal = new TactProduct("anbs") { DisplayName = "Diablo: Immortal", DefaultTags = new[] { "dummy" }, IsBlizzard = true };
        public static readonly TactProduct Diablo2Resurrected = new TactProduct("osi") { DisplayName = "Diablo 2: Resurrected", IsBlizzard = true };
        public static readonly TactProduct Diablo3 = new TactProduct("d3") { DisplayName = "Diablo 3", IsBlizzard = true };
        public static readonly TactProduct Diablo4 = new TactProduct("fenris") { DisplayName = "Diablo 4", IsBlizzard = true };

        public static readonly TactProduct Hearthstone = new TactProduct("hsb") { DisplayName = "Hearthstone", DefaultTags = new[] { "Windows", "enUS" }, IsBlizzard = true };
        public static readonly TactProduct HeroesOfTheStorm = new TactProduct("hero") { DisplayName = "Heroes of the Storm", IsBlizzard = true };

        public static readonly TactProduct Starcraft1 = new TactProduct("s1")
        {
            DisplayName = "Starcraft Remastered",
            DefaultTags = new[] { "Windows", "enUS", "x86", "noigr" },
            IsBlizzard = true
        };

        public static readonly TactProduct Starcraft2 = new TactProduct("s2")
        {
            DisplayName = "Starcraft 2",
            DefaultTags = new[] { "Windows", "enUS" },
            IsBlizzard = true
        };

        public static readonly TactProduct Overwatch2 = new TactProduct("pro")
        {
            DisplayName = "Overwatch 2",
            DefaultTags = new[] { "DevReg", "enUS" },
            IsBlizzard = true
        };

        public static readonly TactProduct Warcraft3Reforged = new TactProduct("w3") { DisplayName = "Warcraft 3: Reforged", IsBlizzard = true };
        public static readonly TactProduct WorldOfWarcraft = new TactProduct("wow")
        {
            DisplayName = "World Of Warcraft",
            DefaultTags = new[] { "Windows", "enUS", "x86" },
            IsBlizzard = true
        };
        public static readonly TactProduct WowClassic = new TactProduct("wow_classic")
        {
            DisplayName = "WoW Classic",
            DefaultTags = new[] { "Windows", "enUS", "x86_64", "US" },
            IsBlizzard = true
        };

        #endregion

        #region Activision

        public static readonly TactProduct CodBO4 = new TactProduct("viper") { DisplayName = "Call of Duty: Black Ops 4", IsActivision = true };
        public static readonly TactProduct CodBOCW = new TactProduct("zeus")
        {
            DisplayName = "Call of Duty: Black Ops Cold War",
            DefaultTags = new[] { "enUS", "acct-DEU", "cp", "mp", "zm", "zm2" },
            IsActivision = true
        };
        public static readonly TactProduct CodModernWarfare = new TactProduct("odin") { DisplayName = "Call of Duty: Modern Warfare 2019", IsActivision = true };

        public static readonly TactProduct CallOfDuty = new TactProduct("auks") { DisplayName = "Call of Duty", IsActivision = true };
        public static readonly TactProduct CodMW2Remastered = new TactProduct("lazr") { DisplayName = "Call of Duty: MW2 Remastered", IsActivision = true };
        public static readonly TactProduct CodVanguard = new TactProduct("fore") { DisplayName = "Call of Duty: Vanguard", IsActivision = true };

        public static readonly TactProduct CrashBandicoot4 = new TactProduct("wlby")
        {
            DisplayName = "Crash Bandicoot 4: It's About Time",
            DefaultTags = new[] { "dummy" },
            IsActivision = true
        };

        #endregion

        /// <summary>
        /// Official name of the game.
        /// </summary>
        public string DisplayName { get; private init; }

        public string DisplayNameSanitized => DisplayName.Replace(":", "");

        /// <summary>
        /// TACT Product code.  Used to find content on Blizzard CDNs.
        /// </summary>
        public string ProductCode => Name;

        internal string[] DefaultTags { get; private init; }

        public bool IsActivision { get; private init; }
        public bool IsBlizzard { get; private init; }

        private TactProduct(string name) : base(name)
        {
        }
    }
}