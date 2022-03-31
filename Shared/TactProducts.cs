namespace Shared
{
    //TODO convert to smart enum
    /// <summary>
    /// From : https://blizztrack.com/
    /// </summary>
    public static class TactProducts 
    {
        //TODO add bnet installer to this 
        
        public static TactProduct Diablo3 = new TactProduct { DisplayName = "Diablo 3", ProductCode = "d3" };

        public static TactProduct Hearthstone = new TactProduct { DisplayName = "Hearthstone", ProductCode = "hsb" };
        public static TactProduct HerosOfTheStorm = new TactProduct { DisplayName = "Heroes of the Storm", ProductCode = "hero" };

        public static TactProduct Starcraft1 = new TactProduct { DisplayName = "Starcraft", ProductCode = "s1" };
        public static TactProduct Starcraft2 = new TactProduct { DisplayName = "Starcraft 2", ProductCode = "s2" };

        public static TactProduct Overwatch = new TactProduct { DisplayName = "Overwatch", ProductCode = "pro" };

        public static TactProduct WowClassic = new TactProduct { DisplayName = "WoW Classic", ProductCode = "wow_classic" };

        #region Activision

        public static readonly TactProduct CodWarzone = new TactProduct { DisplayName = "Call of Duty Warzone", ProductCode = "odin" };
        public static readonly TactProduct CodBlackOpsColdWar = new TactProduct { DisplayName = "Call of Duty Black Ops Cold War", ProductCode = "zeus" };
        public static readonly TactProduct CodVanguard = new TactProduct { DisplayName = "Call of Duty Vanguard", ProductCode = "fore" };

        #endregion
    }
    
    public class TactProduct
    {
        public string DisplayName { get; init; }
        public string ProductCode { get; init; }
    }
}