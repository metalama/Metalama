internal static class C
{
  [TheAspect]
  public static void ClassicStaticExtensionMethod(this TestClass c)
  {
    global::System.Console.WriteLine("Member: C.ClassicStaticExtensionMethod(this TestClass)");
    global::System.Console.WriteLine("Type: C");
    global::System.Console.WriteLine(c);
    Console.WriteLine("Original");
    return;
  }
  extension(TestClass test)
  {
    [TheAspect]
    public void Method()
    {
      global::System.Console.WriteLine("Member: C.extension(TestClass).Method()");
      global::System.Console.WriteLine("Type: C");
      global::System.Console.WriteLine(test);
      Console.WriteLine("Original.");
      return;
    }
    [TheAspect]
    public static void StaticMethod()
    {
      global::System.Console.WriteLine("Member: C.extension(TestClass).StaticMethod()");
      global::System.Console.WriteLine("Type: C");
      Console.WriteLine("Original.");
      return;
    }
    [TheAspect]
    public static TestClass operator *(TestClass vector, float scalar)
    {
      global::System.Console.WriteLine("Member: C.extension(TestClass).operator *(TestClass, float)");
      global::System.Console.WriteLine("Type: C");
      return vector;
    }
    [TheAspect]
    public void operator *=(TestClass scalar)
    {
      global::System.Console.WriteLine("Member: C.extension(TestClass).operator *=(TestClass)");
      global::System.Console.WriteLine("Type: C");
      global::System.Console.WriteLine(test);
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
    test.ClassicStaticExtensionMethod();
    _ = test * 5;
    test *= 10;
  }
}