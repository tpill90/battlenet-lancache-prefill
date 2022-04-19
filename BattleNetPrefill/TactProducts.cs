using BattleNetPrefill.Structs;

namespace BattleNetPrefill
{
    //TODO convert to smart enum
    /// <summary>
    /// A list of all possible games that can be downloaded from Battle.Net.  This list is not comprehensive, as it does not include alpha/beta/PTR versions,
    /// however it does include all games that are currently playable today.
    ///
    /// A complete list can be found at : https://blizztrack.com/
    /// </summary>
    public class TactProducts : EnumBase<TactProducts>
    {
        public string DisplayName { get; init; }
        public string ProductCode => this.Name;

        //TODO convert this to a better type
        //TODO comment
        public string[] DefaultTags { get; init; }

        //TODO make a few "special" products.  Ex. AllProducts, Blizzard products, activision prodcuts

        #region Blizzard

        public static readonly TactProducts BlizzardArcadeCollection = new TactProducts("rtro") { DisplayName = "Blizzard Arcade Collection" };

        public static readonly TactProducts Diablo2Resurrected = new TactProducts("osi") { DisplayName = "Diablo 2: Resurrected" };
        public static readonly TactProducts Diablo3 = new TactProducts("d3") { DisplayName = "Diablo 3" };

        public static readonly TactProducts Hearthstone = new TactProducts("hsb") { DisplayName = "Hearthstone",  DefaultTags = new[] { "Windows", "enUS" } };
        public static readonly TactProducts HeroesOfTheStorm = new TactProducts("hero") { DisplayName = "Heroes of the Storm" };

        public static readonly TactProducts Starcraft1 = new TactProducts("s1") {DisplayName = "Starcraft Remastered" };
        public static readonly TactProducts Starcraft2 = new TactProducts("s2") { DisplayName = "Starcraft 2",  DefaultTags = new[] { "Windows", "enUS" } };

        public static readonly TactProducts Overwatch = new TactProducts("pro") { DisplayName = "Overwatch" };

        public static readonly TactProducts Warcraft3Reforged = new TactProducts("w3") { DisplayName = "Warcraft III: Reforged" };
        public static readonly TactProducts WorldOfWarcraft = new TactProducts("wow") { DisplayName = "World Of Warcraft" };
        public static readonly TactProducts WowClassic = new TactProducts("wow_classic") { DisplayName = "WoW Classic",  DefaultTags = new[] { "Windows", "enUS", "x86_64" } };

        #endregion

        #region Activision

        public static readonly TactProducts CodBO4 = new TactProducts("viper") { DisplayName = "Call of Duty: Black Ops 4" };
        public static readonly TactProducts CodBOCW = new TactProducts("zeus") { DisplayName = "Call of Duty: Black Ops Cold War", DefaultTags = new string[] { "enUS", "cp", "mp", "zm", "zm2" } };
        public static readonly TactProducts CodWarzone = new TactProducts("odin") { DisplayName = "Call of Duty: Modern Warfare" };
        public static readonly TactProducts CodMW2 = new TactProducts("lazr") { DisplayName = "Call of Duty: Modern Warfare 2" };
        public static readonly TactProducts CodVanguard = new TactProducts("fore") { DisplayName = "Call of Duty: Vanguard" };

        public static readonly TactProducts CrashBandicoot4 = new TactProducts("wlby") { DisplayName = "Crash Bandicoot 4: It's About Time" };

        #endregion

        public TactProducts(string name) : base(name)
        {
        }
    }
}