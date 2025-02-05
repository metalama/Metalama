public class TestClass : TestInterface
{
  public void Foo()
  {
  }
  public void Foo(int value)
  {
    global::System.Console.WriteLine("Should be applied only on Foo(int) parameter.");
  }
  public void Bar()
  {
  }
  public void Bar(int value)
  {
    global::System.Console.WriteLine("Should be applied only on Bar(int) method.");
    return;
  }
}