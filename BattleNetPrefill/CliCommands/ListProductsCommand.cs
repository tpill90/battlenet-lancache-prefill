// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace BattleNetPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("list-products", Description = "Lists all available products that can be pre-filled")]
    public sealed class ListProductsCommand : ICommand
    {
        public ValueTask ExecuteAsync(IConsole console)
        {
            var table = new Table
            {
                Border = TableBorder.MinimalHeavyHead
            };
            // Header
            table.AddColumn(new TableColumn(White("Product Name")) { Width = 35 });
            table.AddColumn(new TableColumn(White("ID")));

            // Blizzard
            var blizzMarkup = new Markup("Blizzard", new Style(Color.DodgerBlue1, decoration: Decoration.Bold | Decoration.Underline));
            table.AddRow(blizzMarkup, new Markup(""));
            foreach (var product in TactProduct.AllEnumValues.Where(e => e.IsBlizzard))
            {
                table.AddRow(product.DisplayName, product.ProductCode);
            }
            
            // Activision
            table.AddEmptyRow();
            var activisionMarkup = new Markup("Activision", new Style(Color.Green1, decoration: Decoration.Bold | Decoration.Underline));
            table.AddRow(activisionMarkup, new Markup(""));
            foreach (var product in TactProduct.AllEnumValues.Where(e => e.IsActivision))
            {
                table.AddRow(product.DisplayName, product.ProductCode);
            }

            var ansiConsole = console.CreateAnsiConsole();
            ansiConsole.Write(table);

            return default;
        }
    }
}