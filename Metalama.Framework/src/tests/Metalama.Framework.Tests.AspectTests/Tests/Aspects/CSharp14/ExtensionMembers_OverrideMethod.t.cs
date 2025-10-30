internal static class C
{
  extension(TestClass test)
  {
    [TheAspect]
    public void Method()
    {
      global::System.Console.WriteLine("Override 'C.extension(TestClass).Method()'.");
      global::System.Console.WriteLine(test);
      Console.WriteLine("Original.");
      return;
    }
    [TheAspect]
    public static void StaticMethod()
    {
      global::System.Console.WriteLine("Override 'C.extension(TestClass).StaticMethod()'.");
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