// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.

using Color = Spectre.Console.Color;

//TODO - Remove in the future, no sooner than 2023/08/01 at the minimum
namespace BattleNetPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("list-products", Description = "Deprecated!")]
    public sealed class ListProductsCommand : ICommand
    {
        public ValueTask ExecuteAsync(IConsole console)
        {
            var table = new Table
            {
                ShowHeaders = false,
                Border = TableBorder.Rounded,
                BorderStyle = new Style(Color.Yellow)
            };
            table.AddColumn("");

            // Add some rows
            table.AddRow(LightYellow("Warning!"));
            table.AddRow("");
            table.AddRow($"list-products is being deprecated in favor of {LightBlue("select-apps")}");
            table.AddRow("and will be removed in a future release!");
            table.AddRow("Download at :  ");

            // Render the table to the console
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            return default;
        }
    }
}