public class Caller
{
  public void Method()
  {
    var t = new TargetCode
    {
      Value = 42
    }.WithInitialize();
  }
}