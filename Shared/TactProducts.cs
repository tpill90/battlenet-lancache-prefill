
namespace Shared
{
    public static class TactProducts 
    {
        public static TactProduct Diablo3 = new TactProduct { DisplayName = "Diablo 3", ProductCode = "d3" };

        public static TactProduct Starcraft1 = new TactProduct { DisplayName = "Starcraft", ProductCode = "s1" };
        public static TactProduct Starcraft2 = new TactProduct { DisplayName = "Starcraft 2", ProductCode = "s2" };

        public static TactProduct Hearthstone = new TactProduct { DisplayName = "Hearthstone", ProductCode = "hsb" };

        public static TactProduct HerosOfTheStorm = new TactProduct { DisplayName = "Heroes of the Storm", ProductCode = "hero" };

        public static TactProduct Overwatch = new TactProduct { DisplayName = "Overwatch", ProductCode = "pro" };

        public static TactProduct WowClassic = new TactProduct { DisplayName = "WoW Classic", ProductCode = "wow_classic" };

        //TODO test if this works
        public static TactProduct CodWarzone = new TactProduct { DisplayName = "Call of Duty Warzone", ProductCode = "odin" };
    }
    

    public class TactProduct
    {
        public string DisplayName { get; init; }
        public string ProductCode { get; init; }
    }
}
