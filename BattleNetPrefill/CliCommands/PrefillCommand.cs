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

        [CommandOption("unit",
            Description = "Specifies which unit to use to display download speed.  Can be either bits/bytes.",
            Converter = typeof(TransferSpeedUnitConverter))]
        public TransferSpeedUnit TransferSpeedUnit
        {
            get => AppConfig.TransferSpeedUnit;
            init => AppConfig.TransferSpeedUnit = value ?? TransferSpeedUnit.Bits;
        }

        [CommandOption("no-ansi",
            Description = "Application output will be in plain text.  " +
                          "Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoAnsiEscapeSequences { get; init; }

        private IAnsiConsole _ansiConsole;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();
            // Property must be set to false in order to disable ansi escape sequences
            _ansiConsole.Profile.Capabilities.Ansi = !NoAnsiEscapeSequences ?? true;

            await UpdateChecker.CheckForUpdatesAsync(typeof(Program), "tpill90/battlenet-lancache-prefill", AppConfig.CacheDir);

            ValidateUserHasSelectedApps();

            try
            {
                var timer = Stopwatch.StartNew();
                List<TactProduct> productsToProcess = BuildProductListFromArgs();
                _ansiConsole.LogMarkupLine($"Prefilling {LightYellow(productsToProcess.Count)} products \n");

                foreach (var code in productsToProcess.Distinct().ToList())
                {
                    try
                    {
                        var tactProductHandler = new TactProductHandler(code, _ansiConsole);
                        await tactProductHandler.ProcessProductAsync(NoLocalCache ?? default(bool), ForcePrefill ?? default(bool));
                    }
                    catch (Exception e)
                    {
                        // Need to catch any exceptions that might happen during a single download, so that the other apps won't be affected
                        _ansiConsole.LogMarkupLine(Red($"Unexpected download error : {e.Message}  Skipping app..."));
                        _ansiConsole.MarkupLine("");
                    }

                }

                _ansiConsole.LogMarkupLine($"Prefill complete! Prefilled {Magenta(productsToProcess.Count)} apps", timer);
            }
            //TODO will probably need to implement this so that clifx properly displays the help text
            catch (CommandException)
            {
                throw;
            }
            catch (Exception e)
            {
                _ansiConsole.LogException(e);
            }
        }

        private List<TactProduct> BuildProductListFromArgs()
        {
            // Start by loading any selected apps
            var productsToProcess = TactProductHandler.LoadPreviouslySelectedApps();

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

            return productsToProcess.Distinct().ToList();
        }

        // Validates that the user has selected at least 1 app
        private void ValidateUserHasSelectedApps()
        {
            var userSelectedApps = TactProductHandler.LoadPreviouslySelectedApps();
            if ((PrefillAllProducts ?? default(bool)) || (PrefillActivision ?? default(bool)) || (PrefillBlizzard ?? default(bool))
                    || (ProductCodes != null && ProductCodes.Any()) || userSelectedApps.Any())
            {
                return;
            }

            _ansiConsole.MarkupLine(Red("No apps have been selected for prefill! At least 1 app is required!"));
            _ansiConsole.MarkupLine(Red($"Use the {Cyan("select-apps")} command to interactively choose which apps to prefill. "));
            _ansiConsole.MarkupLine("");
            _ansiConsole.Markup(Red($"Alternatively, the flag {LightYellow("--all")} can be specified to prefill all owned apps"));
            _ansiConsole.Markup(Red($"or use {LightYellow("--activision")}, or {LightYellow("--blizzard")} to load predefined groups"));
            throw new CommandException(".", 1, true);
        }
    }
}