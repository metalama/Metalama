[IntroductionAspect]
public class TargetClass
{
  [InvokerAspect]
  public int Invoker
  {
    get
    {
      // Invoke instance.Field
      _ = this.Field;
      // Invoke instance.Field
      _ = this.Field;
      // Invoke instance.Field
      _ = this.Field;
      return 0;
    }
    set
    { // Invoke instance.Field
      this.Field = 42;
      // Invoke instance.Field
      this.Field = 42;
      // Invoke instance.Field
      this.Field = 42;
    }
  }
  public global::System.Int32 Field;
}