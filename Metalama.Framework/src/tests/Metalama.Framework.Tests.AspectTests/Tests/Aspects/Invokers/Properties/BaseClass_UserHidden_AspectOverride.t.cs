public class TargetClass : BaseClass
{
  [OverrideAspect]
  public new int Property
  {
    get
    {
      // Invoke this.Property_Source
      _ = this.Property_Source;
      // Invoke this.Property_Source
      _ = this.Property_Source;
      // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property_Source
      return this.Property_Source;
    }
    set
    { // Invoke this.Property_Source
      this.Property_Source = 42;
      // Invoke this.Property_Source
      this.Property_Source = 42;
      // Invoke this.Property
      this.Property = 42;
      // Invoke this.Property
      this.Property = 42;
      // Invoke this.Property_Source
      this.Property_Source = value;
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
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    {
      // Invoke this.Property
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
  [InvokerAfterAspect]
  public int InvokerAfter
  {
    get
    {
      // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property
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