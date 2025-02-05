public class TargetClass
{
  private DifferentClass? instance;
  [InvokerAspect]
  public int Invoker
  {
    get
    {
      // Invoke instance.Property
      _ = this.instance.Property;
      // Invoke instance?.Property
      _ = this.instance?.Property;
      // Invoke instance.Property
      _ = this.instance.Property;
      // Invoke instance?.Property
      _ = this.instance?.Property;
      return 0;
    }
    set
    { // Invoke instance.Property
      this.instance.Property = 42;
      // Invoke instance.Property
      this.instance.Property = 42;
    }
  }
}