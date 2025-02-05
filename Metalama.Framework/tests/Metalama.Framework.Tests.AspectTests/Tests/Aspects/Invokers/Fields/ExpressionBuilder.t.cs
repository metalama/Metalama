internal class TargetClass
{
  public int F;
  [Test]
  public void Map(TargetClass source, TargetClass target)
  {
    target.F = source.F;
    return;
  }
}