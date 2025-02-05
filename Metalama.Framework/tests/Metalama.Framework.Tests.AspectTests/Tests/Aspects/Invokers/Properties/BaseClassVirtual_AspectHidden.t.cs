[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    {
      // Invoke this.Property
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
  public new global::System.Int32 Property
  {
    get
    {
      // Invoke base.Property
      _ = base.Property;
      // Invoke base.Property
      _ = base.Property;
      // Invoke this.Property
      _ = this.Property;
      // Invoke this.Property
      _ = this.Property;
      // Invoke base.Property
      return base.Property;
    }
    set
    {
      // Invoke base.Property
      base.Property = 42;
      // Invoke base.Property
      base.Property = 42;
      // Invoke this.Property
      this.Property = 42;
      // Invoke this.Property
      this.Property = 42;
      // Invoke base.Property
      base.Property = value;
    }
  }
}