public class Caller
{
  public void Method()
  {
    // User already supplies InitializationContext — linker should NOT add another.
    var t = new TargetCode(42, InitializationContext.WillInitialize);
  }
}