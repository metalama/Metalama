[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    {
      // Invoke base.Field
      _ = base.Field;
      return 0;
    }
    set
    { // Invoke base.Field
      base.Field = 42;
    }
  }
  public new global::System.Int32 Field;
}
