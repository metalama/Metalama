public class Caller
{
  public void Method()
  {
    var b = new BaseClass(1, context: InitializationContext.WillInitialize).WithInitialize();
    var d = new DerivedClass(2, context: InitializationContext.WillInitialize).WithInitialize();
  }
}