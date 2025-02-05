public class TargetClass : BaseClass
{
  [InvokerAspect]
  public int Invoker
  {
    get
    { // Invoke this.Property
      _ = this.Property;
      // Invoke base.Property
      _ = base.Property;
      // Invoke base.Property
      _ = base.Property;
      // Invoke this.Property
      _ = this.Property;
      return 0;
    }
    set
    { // Invoke this.Property
      this.Property = 42;
      // Invoke base.Property
      base.Property = 42;
      // Invoke base.Property
      base.Property = 42;
      // Invoke this.Property
      this.Property = 42;
    }
  }
}