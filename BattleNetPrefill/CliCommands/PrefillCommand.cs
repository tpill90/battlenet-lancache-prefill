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

        [CommandOption("verbose", Description = "Produces more detailed log output. Will output logs for games are already up to date.", Converter = typeof(NullableBoolConverter))]
        public bool? Verbose
        {
            get => AppConfig.VerboseLogs;
            init => AppConfig.VerboseLogs = value ?? default(bool);
        }

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

            var tactProductHandler = new TactProductHandler(_ansiConsole, NoLocalCache ?? default(bool), ForcePrefill ?? default(bool));

            List<TactProduct> productsToProcess = BuildProductListFromArgs();
            await tactProductHandler.ProcessMultipleProductsAsync(productsToProcess);
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