[TestAspect]
public class Target
{
  public int Foo()
  {
    if (Foo_Source() > 0)
    {
      var z = this.Foo_Source();
      return z - 1;
    }
    return 0;
  }
  private int Foo_Source()
  {
    return 10;
  }
}