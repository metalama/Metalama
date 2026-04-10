public class Caller
{
  public void Method()
  {
    var x = new X
    {
      Y = new Y
      {
        Z = new Z().WithInitialize()
      }.WithInitialize()
    }.WithInitialize();
  }
}