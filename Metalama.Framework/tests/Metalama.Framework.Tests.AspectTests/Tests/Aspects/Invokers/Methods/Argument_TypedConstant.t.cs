internal class TargetClass
{
  [Test]
  private void M(int i, int j)
  {
    if (i == 0)
    {
      this.M_Source(42, 42);
      return;
    }
    else
    {
      this.M_Source(i, j);
      return;
    }
  }
  private void M_Source(int i, int j) => Console.WriteLine(i + j);
}