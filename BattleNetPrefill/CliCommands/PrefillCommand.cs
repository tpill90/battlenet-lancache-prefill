using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BattleNetPrefill.Extensions;
using BattleNetPrefill.Utils;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Spectre.Console;
using static BattleNetPrefill.Utils.SpectreColors;

// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace BattleNetPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("prefill", Description = "Downloads the latest files for one or more specified product(s)")]
    public sealed class PrefillCommand : ICommand
    {
        [CommandOption("products", shortName: 'p', 
            Description = "Specifies which products to prefill.  Example '--products s1' will prefill Starcraft 1", 
            Converter = typeof(TactProductConverter))]
        public IReadOnlyList<TactProduct> ProductCodes { get; init; }

        [CommandOption("all", Description = "Prefills all available products.  Includes all Activision and Blizzard games", Converter = typeof(NullableBoolConverter))]
        public bool? PrefillAllProducts { get; init; }

        [CommandOption("activision", Description = "Prefills all Activision products.", Converter = typeof(NullableBoolConverter))]
        public bool? PrefillActivision { get; init; }

        [CommandOption("blizzard", Description = "Prefills all Blizzard products.", Converter = typeof(NullableBoolConverter))]
        public bool? PrefillBlizzard { get; init; }

        [CommandOption("nocache", 
            Description = "Skips using locally cached files.  Saves disk space, at the expense of slower subsequent runs.", 
            Converter = typeof(NullableBoolConverter))]
        public bool? NoLocalCache { get; init; }

        [CommandOption("force", shortName: 'f', 
            Description = "Forces the prefill to always run, overrides the default behavior of only prefilling if a newer version is available.", 
            Converter = typeof(NullableBoolConverter))]
        public bool? ForcePrefill { get; init; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            var ansiConsole = console.CreateAnsiConsole();
            try
            {
                var timer = Stopwatch.StartNew();

                await UpdateChecker.CheckForUpdatesAsync();

                List<TactProduct> productsToProcess = BuildProductListFromArgs();

                if (productsToProcess.Count == 0)
                {
                    throw new CommandException("At least one product is required!  Use '--products' to specify which products to load, " +
                                               "or use bulk flags '--all', '--activision', or '--blizzard' to load predefined groups", 1, true);
                }
                
                ansiConsole.LogMarkupLine($"Prefilling {LightYellow(productsToProcess.Count)} products \n");
                foreach (var code in productsToProcess.Distinct().ToList())
                {
                    var tactProductHandler = new TactProductHandler(code, ansiConsole, AppConfig.DebugConfig);
                    await tactProductHandler.ProcessProductAsync(NoLocalCache ?? default(bool), ForcePrefill ?? default(bool));
                }

                ansiConsole.LogMarkupLine($"Prefill complete! Prefilled {Magenta(productsToProcess.Count)} apps in {LightYellow(timer.FormatElapsedString())}");
            }
            catch (Exception e)
            {
                ansiConsole.WriteException(e, ExceptionFormats.ShortenPaths);
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
            if (PrefillAllProducts ?? default(bool))
            {
                productsToProcess.AddRange(TactProduct.AllEnumValues);
            }
            // --activision flag
            if (PrefillActivision ?? default(bool))
            {
                productsToProcess.AddRange(TactProduct.AllEnumValues.Where(e => e.IsActivision));
            }
            // --blizzard flag
            if (PrefillBlizzard ?? default(bool))
            {
                productsToProcess.AddRange(TactProduct.AllEnumValues.Where(e => e.IsBlizzard));
            }

            return productsToProcess;
        }
    }
}