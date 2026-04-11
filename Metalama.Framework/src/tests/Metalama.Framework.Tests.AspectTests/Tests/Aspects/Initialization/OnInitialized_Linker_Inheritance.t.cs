public class Caller
{
  public void Method()
  {
    var b = new BaseClass().WithInitialize();
    var d = new DerivedClass().WithInitialize();
  }
}