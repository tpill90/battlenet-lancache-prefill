using BattleNetPrefill.Structs;

namespace BattleNetPrefill
{
    /// <summary>
    /// A list of all possible games that can be downloaded from Battle.Net.  This list is not comprehensive, as it does not include alpha/beta/PTR versions,
    /// however it does include all games that are currently playable today.
    ///
    /// A complete list can be found at : https://blizztrack.com/
    /// </summary>
    public class TactProduct : EnumBase<TactProduct>
    {
        //TODO make a few "special" products.  Ex. AllProducts, Blizzard products, activision prodcuts
        #region Blizzard

        //TODO throws download errors
        public static readonly TactProduct BlizzardArcadeCollection = new TactProduct("rtro") { DisplayName = "Blizzard Arcade Collection", DefaultTags = new[] { "dummy" }, IsBlizzard = true };

        public static readonly TactProduct Diablo2Resurrected = new TactProduct("osi") { DisplayName = "Diablo 2: Resurrected", IsBlizzard = true };
        public static readonly TactProduct Diablo3 = new TactProduct("d3") { DisplayName = "Diablo 3", IsBlizzard = true };

        public static readonly TactProduct Hearthstone = new TactProduct("hsb") { DisplayName = "Hearthstone", DefaultTags = new[] { "Windows", "enUS" }, IsBlizzard = true };
        public static readonly TactProduct HeroesOfTheStorm = new TactProduct("hero") { DisplayName = "Heroes of the Storm", IsBlizzard = true };

        public static readonly TactProduct Starcraft1 = new TactProduct("s1") {DisplayName = "Starcraft Remastered", IsBlizzard = true };
        public static readonly TactProduct Starcraft2 = new TactProduct("s2") { DisplayName = "Starcraft 2", DefaultTags = new[] { "Windows", "enUS" }, IsBlizzard = true };

        public static readonly TactProduct Overwatch = new TactProduct("pro") { DisplayName = "Overwatch", IsBlizzard = true };

        public static readonly TactProduct Warcraft3Reforged = new TactProduct("w3") { DisplayName = "Warcraft 3: Reforged", IsBlizzard = true };
        public static readonly TactProduct WorldOfWarcraft = new TactProduct("wow") { DisplayName = "World Of Warcraft", IsBlizzard = true };
        public static readonly TactProduct WowClassic = new TactProduct("wow_classic") { DisplayName = "WoW Classic", DefaultTags = new[] { "Windows", "enUS", "x86_64" }, IsBlizzard = true };

        #endregion

        #region Activision

        //TODO throws download errors
        public static readonly TactProduct CodBO4 = new TactProduct("viper") { DisplayName = "Call of Duty: Black Ops 4", IsActivision = true };
        //TODO throws download errors
        public static readonly TactProduct CodBOCW = new TactProduct("zeus") { DisplayName = "Call of Duty: Black Ops Cold War", DefaultTags = new string[] { "enUS", "cp", "mp", "zm", "zm2" }, IsActivision = true };
        //TODO throws download errors
        public static readonly TactProduct CodWarzone = new TactProduct("odin") { DisplayName = "Call of Duty: Modern Warfare", IsActivision = true };
        //TODO throws download errors
        public static readonly TactProduct CodMW2 = new TactProduct("lazr") { DisplayName = "Call of Duty: Modern Warfare 2", IsActivision = true };
        //TODO throws download errors
        public static readonly TactProduct CodVanguard = new TactProduct("fore") { DisplayName = "Call of Duty: Vanguard", IsActivision = true };

        //TODO doesn't work
        public static readonly TactProduct CrashBandicoot4 = new TactProduct("wlby") { DisplayName = "Crash Bandicoot 4: It's About Time", DefaultTags = new[] { "dummy" }, IsActivision = true };

        #endregion

        //TODO document properties
        public string DisplayName { get; init; }
        public string ProductCode => this.Name;

        //TODO convert this to a better type
        //TODO comment
        public string[] DefaultTags { get; init; }

        public bool IsActivision { get; init; }
        public bool IsBlizzard { get; init; }

        private TactProduct(string name) : base(name)
        {
        }
    }
}