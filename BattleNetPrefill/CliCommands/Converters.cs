namespace BattleNetPrefill.CliCommands
{
    public class TactProductConverter : BindingConverter<TactProduct>
    {
        public override TactProduct Convert(string rawValue)
        {
            return TactProduct.Parse(rawValue);
        }
    }


}
