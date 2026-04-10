public class Caller
{
  public void Method()
  {
    var t = new MyType
    {
      Value = 42
    }.WithInitialize();
  }
}