[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    { // Invoke this.Field
      _ = this.Field;
      // Invoke base.Field
      _ = base.Field;
      // Invoke base.Field
      _ = base.Field;
      // Invoke this.Field
      _ = this.Field;
      return 0;
    }
    set
    { // Invoke this.Field
      this.Field = 42;
      // Invoke base.Field
      base.Field = 42;
      // Invoke base.Field
      base.Field = 42;
      // Invoke this.Field
      this.Field = 42;
    }
  }
  [InvokerAfterAspect]
  public int InvokerAfter
  {
    get
    { // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      return 0;
    }
    set
    { // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
    }
  }
  public new global::System.Int32 Field;
}
