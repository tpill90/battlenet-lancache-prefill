namespace BattleNetPrefill.CliCommands
{
    public class TactProductConverter : BindingConverter<TactProduct>
    {
        public override TactProduct Convert(string rawValue)
        {
            return TactProduct.Parse(rawValue);
        }
    }

    public class NullableBoolConverter : BindingConverter<bool?>
    {
        // Required in order to prevent CliFx from showing the unnecessary 'Default: "False"' text for boolean flags
        public override bool? Convert(string rawValue)
        {
            return true;
        }
    }
}
