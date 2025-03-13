public class TargetClass : BaseClass
{
  public new int Property
  {
    get
    {
      return 0;
    }
    set
    {
    }
  }
  [InvokerAspect]
  public int Invoker
  {
    get
    { // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property
      _ = this.Property;
      return 0;
    }
    set
    { // Invoke this.Property
      this.Property = 42;
      // Invoke this.Property
      this.Property = 42;
      // Invoke this.Property
      this.Property = 42;
      // Invoke this.Property
      this.Property = 42;
    }
  }
}