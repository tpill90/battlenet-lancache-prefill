using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BattleNetPrefill.Utils;
using CliFx;
using CliFx.Attributes;
using CliFx.Extensibility;
using CliFx.Infrastructure;
using Spectre.Console;

namespace BattleNetPrefill
{
    public class CliCommands
    {
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
                table.AddColumn(new TableColumn(SpectreColors.Blue("ID")));

                foreach (var product in TactProduct.AllEnumValues)
                {
                    table.AddRow(product.DisplayName, product.ProductCode);
                }

                AnsiConsole.Write(table);


                return default;
            }
        }

        [Command("prefill", Description = "Downloads the latest files for a specified product(s)")]
        public class PrefillCommand : ICommand
        {
            [CommandOption("products", IsRequired = true, Converter = typeof(TactProductConverter))]
            public IReadOnlyList<TactProduct> ProductCodes { get; init; }

            //TODO comment
            [CommandOption("no-local-cache")]
            public bool NoLocalCache { get; init; }

            public ValueTask ExecuteAsync(IConsole console)
            {
                foreach (var code in ProductCodes.Distinct().ToList())
                {
                    TactProductHandler.ProcessProduct(code, AnsiConsole.Create(new AnsiConsoleSettings()),
                        Config.UseCdnDebugMode, Config.WriteOutputFiles, Config.ShowDebugStats, NoLocalCache);
                }

                return default;
            }

        }

        //TODO comment
        [Command("prefill-all")]
        public class PrefillAllCommand : ICommand
        {
            //TODO comment
            [CommandOption("no-local-cache")]
            public bool NoLocalCache { get; init; }

            public ValueTask ExecuteAsync(IConsole console)
            {
                foreach (var code in TactProduct.AllEnumValues)
                {
                    TactProductHandler.ProcessProduct(code, AnsiConsole.Create(new AnsiConsoleSettings()),
                        Config.UseCdnDebugMode, Config.WriteOutputFiles, Config.ShowDebugStats, NoLocalCache);
                }
                return default;
            }

        }

        public class TactProductConverter : BindingConverter<TactProduct>
        {
            public override TactProduct Convert(string? rawValue)
            {
                if (string.IsNullOrWhiteSpace(rawValue))
                    return default;
                
                return TactProduct.Parse(rawValue);
            }
        }
    }
}