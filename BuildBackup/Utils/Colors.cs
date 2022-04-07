namespace Shared
{
    public static class Colors
    {
        //TODO see if this kind of stuff can be pulled into the main Crayon package
        public static string Cyan(object value)
        {
            return Crayon.Output.Cyan(value.ToString());
        }

        public static string Green(object value)
        {
            return Crayon.Output.Green(value.ToString());
        }

        public static string Magenta(object value)
        {
            return Crayon.Output.Magenta(value.ToString());
        }

        public static string Red(object value)
        {
            return Crayon.Output.Red(value.ToString());
        }

        public static string Yellow(object value)
        {
            return Crayon.Output.Yellow(value.ToString());
        }
    }

    public static class SpectreColors
    {
        public static string Blue(string inputText)
        {
            return $"[blue]{inputText}[/]";
        }
    }
}