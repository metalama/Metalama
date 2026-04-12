public class Caller
{
  public void Method()
  {
    var t = new TargetCode(42, context: InitializationContext.WillInitialize).WithInitialize();
  }
}