internal static class C
{
  extension(TestClass test)
  {
    [TheAspect]
    public void Method()
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original.");
      return;
    }
    [TheAspect]
    public static void StaticMethod()
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original.");
      return;
    }
  }
}
internal class Test
{
  public void Foo()
  {
    var test = new TestClass();
    test.Method();
    TestClass.StaticMethod();
  }
}