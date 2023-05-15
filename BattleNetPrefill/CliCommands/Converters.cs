namespace BattleNetPrefill.CliCommands
{
    public sealed class TactProductConverter : BindingConverter<TactProduct>
    {
        public override TactProduct Convert(string rawValue)
        {
            return TactProduct.Parse(rawValue);
        }
    }
}
