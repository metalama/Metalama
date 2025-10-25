public class TargetClass
{
  public int Property
  {
    get
    {
      return 0;
    }
    set
    {
    }
  }
  private TargetClass? instance;
  [InvokerAspect]
  public int Invoker
  {
    get
    {
      _ = this.instance.Property;
      return 0;
    }
    set
    {
      this.instance.Property = 42;
    }
  }
}