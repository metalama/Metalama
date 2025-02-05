[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    {
      // Invoke this.Field
      _ = this.Field;
      // Invoke base.Field
      _ = this.Field_Empty;
      // Invoke base.Field
      _ = this.Field_Empty;
      // Invoke this.Field
      _ = this.Field;
      return 0;
    }
    set
    { // Invoke this.Field
      this.Field = 42;
      // Invoke base.Field
      this.Field_Empty = 42;
      // Invoke base.Field
      this.Field_Empty = 42;
      // Invoke this.Field
      this.Field = 42;
    }
  }
  [InvokerAfterAspect]
  public int InvokerAfter
  {
    get
    {
      // Invoke this.Field
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
  private global::System.Int32 Field_Empty
  {
    get => default(global::System.Int32);
    set
    {
    }
  }
  public new global::System.Int32 Field;
}