using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleNetPrefill.Utils;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Extensibility;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Spectre.Console;
using static BattleNetPrefill.Utils.SpectreColors;
// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.

namespace BattleNetPrefill
{
    [UsedImplicitly]
    public class CliCommands
    {
        [UsedImplicitly]
        [Command("list-products", Description = "Lists all available products that can be pre-filled")]
        public class ListProductsCommand : ICommand
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

        [UsedImplicitly]
        [Command("prefill", Description = "Downloads the latest files for one or more specified product(s)")]
        public class PrefillCommand : ICommand
        {
            [CommandOption("products", shortName: 'p', Description = "Specifies which products to prefill.  Example '--products s1' will prefill Starcraft 1", Converter = typeof(TactProductConverter))]
            public IReadOnlyList<TactProduct> ProductCodes { get; init; }

            [CommandOption("all", Description = "Prefills all available products.  Includes all Activision and Blizzard games")]
            public bool PrefillAllProducts { get; init; }

            [CommandOption("activision", Description = "Prefills all Activision products.")]
            public bool PrefillActivision { get; init; }

            [CommandOption("blizzard", Description = "Prefills all Blizzard products.")]
            public bool PrefillBlizzard { get; init; }

            [CommandOption("nocache", Description = "Skips using locally cached files.  Saves disk space, at the expense of slower subsequent runs.")]
            public bool NoLocalCache { get; init; }
            
            [CommandOption("force", shortName: 'f', Description = "Forces the prefill to always run, overrides the default behavior of only prefilling if a newer version is available.")]
            public bool ForcePrefill { get; init; }

            public async ValueTask ExecuteAsync(IConsole console)
            {
                List<TactProduct> productsToProcess = BuildProductListFromArgs();

                if (productsToProcess.Count == 0)
                {
                    throw new CommandException("At least one product is required!  Use '--products' to specify which products to load, " +
                                               "or use bulk flags '--all', '--activision', or '--blizzard' to load predefined groups", 1, true);
                }

                var ansiConsole = console.CreateAnsiConsole();
                ansiConsole.MarkupLine($"Prefilling {Yellow(productsToProcess.Count)} products");
                foreach (var code in productsToProcess.Distinct().ToList())
                {
                    var tactProductHandler = new TactProductHandler(code, ansiConsole, Config.DebugConfig);
                    await tactProductHandler.ProcessProductAsync(NoLocalCache, ForcePrefill);
                }
            }

            private List<TactProduct> BuildProductListFromArgs()
            {
                var productsToProcess = new List<TactProduct>();
                // -p flag
                if (ProductCodes != null)
                {
                    productsToProcess.AddRange(ProductCodes);
                }
                // --all flag
                if (PrefillAllProducts)
                {
                    productsToProcess.AddRange(TactProduct.AllEnumValues);
                }
                // --activision flag
                if (PrefillActivision)
                {
                    productsToProcess.AddRange(TactProduct.AllEnumValues.Where(e => e.IsActivision));
                }
                // --blizzard flag
                if (PrefillBlizzard)
                {
                    productsToProcess.AddRange(TactProduct.AllEnumValues.Where(e => e.IsBlizzard));
                }

                return productsToProcess;
            }
        }

        private class TactProductConverter : BindingConverter<TactProduct>
        {
            public override TactProduct Convert(string rawValue)
            {
                return TactProduct.Parse(rawValue);
            }
        }
    }
}