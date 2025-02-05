public class TargetClass : BaseClass
{
  public override int Property
  {
    get
    {
      return this.Property;
    }
    set
    {
      this.Property = value;
    }
  }
  private int Property_Source
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
      // Invoke this.Property_Source
      _ = this.Property_Source;
      // Invoke this.Property_Source
      _ = this.Property_Source;
      // Invoke this.Property
      _ = this.Property;
      return 0;
    }
    set
    { // Invoke this.Property
      this.Property = 42;
      // Invoke this.Property_Source
      this.Property_Source = 42;
      // Invoke this.Property_Source
      this.Property_Source = 42;
      // Invoke this.Property
      this.Property = 42;
    }
  }
}