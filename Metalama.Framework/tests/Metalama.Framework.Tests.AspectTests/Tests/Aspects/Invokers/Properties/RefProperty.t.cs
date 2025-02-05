internal class TargetClass
{
  private void M(ref int i)
  {
  }
  public int F;
  public ref int P => ref F;
  [Test]
  public void Map(TargetClass source)
  {
    this.M(ref this.F);
    this.M(ref this.P);
    return;
  }
}