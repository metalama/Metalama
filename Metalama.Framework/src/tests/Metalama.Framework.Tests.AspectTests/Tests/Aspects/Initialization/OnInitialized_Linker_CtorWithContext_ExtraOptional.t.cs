public class Caller
{
  public void Method()
  {
    // Only required arg supplied — named arg must skip optional 'name'.
    var t1 = new TargetCode(42, context: InitializationContext.WillInitialize).WithInitialize();
    // Both positional args supplied — named arg appended after 'name'.
    var t2 = new TargetCode(42, "hello", context: InitializationContext.WillInitialize).WithInitialize();
  }
}