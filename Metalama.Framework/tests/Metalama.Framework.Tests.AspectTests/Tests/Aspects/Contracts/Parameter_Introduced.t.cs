[Test]
internal class Target
{
  public Target(global::System.Int32 dependency = 0)
  {
    if (dependency == 0)
    {
      throw new global::System.ArgumentOutOfRangeException("dependency");
    }
  }
  public Target(int x, global::System.Int32 dependency = 0)
  {
    if (dependency == 0)
    {
      throw new global::System.ArgumentOutOfRangeException("dependency");
    }
  }
}