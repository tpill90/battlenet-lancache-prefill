namespace BattleNetPrefill.Test;

[TestFixture]
public class TactProductTests
{
    [Test]
    public void Parse_WowForever_ReturnsWowForeverProduct()
    {
        var product = TactProduct.Parse("wowf");

        Assert.That(product, Is.SameAs(TactProduct.WowForever));
        Assert.That(product.DisplayName, Is.EqualTo("WoW: Forever"));
        Assert.That(product.IsBlizzard, Is.True);
    }
}
