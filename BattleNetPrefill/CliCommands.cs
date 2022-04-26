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

namespace BattleNetPrefill
{
    [UsedImplicitly]
    public class CliCommands
    {
        [UsedImplicitly]
        [Command("list-products", Description = "Lists all available products that can be pre-filled")]
        public class ListProductsCommand : ICommand
        {
            //TODO make this look nicer
            public ValueTask ExecuteAsync(IConsole console)
            {
                AnsiConsole.WriteLine();

                // Header
                var table = new Table();
                table.AddColumn(new TableColumn("Product Name"));
                table.AddColumn(new TableColumn(Blue("ID")));

                foreach (var product in TactProduct.AllEnumValues)
                {
                    table.AddRow(product.DisplayName, product.ProductCode);
                }

                AnsiConsole.Write(table);


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

            public ValueTask ExecuteAsync(IConsole console)
            {
                List<TactProduct> productsToProcess = BuildProductListFromArgs();

                if (productsToProcess.Count == 0)
                {
                    throw new CommandException("At least one product is required!  Use '--products' to specify which products to load, " +
                                               "or use bulk flags '--all', '--activision', or '--blizzard' to load predefined groups", 1, true);
                }

                AnsiConsole.MarkupLine($"Prefilling {Yellow(productsToProcess.Count)} products");
                foreach (var code in productsToProcess.Distinct().ToList())
                {
                    TactProductHandler.ProcessProductAsync(code, AnsiConsole.Create(new AnsiConsoleSettings()),
                        Config.UseCdnDebugMode, Config.WriteOutputFiles, Config.ShowDebugStats, NoLocalCache).Wait();
                }

                return default;
            }

            private List<TactProduct> BuildProductListFromArgs()
            {
                var productsToProcess = new List<TactProduct>();
                if (ProductCodes != null)
                {
                    productsToProcess.AddRange(ProductCodes);
                }
                if (PrefillAllProducts)
                {
                    productsToProcess.AddRange(TactProduct.AllEnumValues);
                }
                if (PrefillActivision)
                {
                    productsToProcess.AddRange(TactProduct.AllEnumValues.Where(e => e.IsActivision));
                }
                if (PrefillBlizzard)
                {
                    productsToProcess.AddRange(TactProduct.AllEnumValues.Where(e => e.IsBlizzard));
                }

                return productsToProcess;
            }
        }

        public class TactProductConverter : BindingConverter<TactProduct>
        {
            public override TactProduct Convert(string rawValue)
            {
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    //TODO test this
                    return default;
                }
                
                return TactProduct.Parse(rawValue);
            }
        }
    }
}