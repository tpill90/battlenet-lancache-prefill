namespace BuildBackup
{
    //TODO convert to smart enum
    /// <summary>
    /// From : https://blizztrack.com/
    /// </summary>
    public static class TactProducts 
    {
        //TODO eventually get the rest of blizzards games, for testing

        public static readonly TactProduct Diablo3 = new TactProduct { DisplayName = "Diablo 3", ProductCode = "d3" };

        public static readonly TactProduct Hearthstone = new TactProduct { DisplayName = "Hearthstone", ProductCode = "hsb" };
        public static readonly TactProduct HeroesOfTheStorm = new TactProduct { DisplayName = "Heroes of the Storm", ProductCode = "hero" };

        public static readonly TactProduct Starcraft1 = new TactProduct { DisplayName = "Starcraft", ProductCode = "s1" };
        public static readonly TactProduct Starcraft2 = new TactProduct { DisplayName = "Starcraft 2", ProductCode = "s2" };

        public static readonly TactProduct Overwatch = new TactProduct { DisplayName = "Overwatch", ProductCode = "pro" };

        public static readonly TactProduct WorldOfWarcraft = new TactProduct { DisplayName = "World Of Warcraft", ProductCode = "wow" };
        public static readonly TactProduct WowClassic = new TactProduct
        {
            DisplayName = "WoW Classic", 
            ProductCode = "wow_classic", 
            DefaultTags = new string[]{ "Windows", "enUS" }
        };
        
        #region Activision

        public static readonly TactProduct CodWarzone = new TactProduct { DisplayName = "Call of Duty Warzone", ProductCode = "odin" };
        public static readonly TactProduct CodBlackOpsColdWar = new TactProduct
        {
            DisplayName = "Call of Duty Black Ops Cold War", 
            ProductCode = "zeus",
            DefaultTags = new string[] {
                "enUS",
                "cp",
                "mp",
                "zm",
                "zm2"
            }
        };
        public static readonly TactProduct CodVanguard = new TactProduct { DisplayName = "Call of Duty Vanguard", ProductCode = "fore" };

        #endregion
    }
    
    public class TactProduct
    {
        public string DisplayName { get; init; }
        public string ProductCode { get; init; }

        //TODO convert this to a better type
        //TODO comment
        public string[] DefaultTags { get; init; }
    }
}