internal static class C
{
  public static void ClassicStaticExtensionMethod([TheAspect] this TestClass c)
  {
    global::System.Console.WriteLine("Member: C.ClassicStaticExtensionMethod(this TestClass)");
    global::System.Console.WriteLine("Type: C");
    global::System.Console.WriteLine(c);
    Console.WriteLine("Original");
  }
  extension(TestClass test)
  {
    public static TestClass operator *([TheAspect] TestClass vector, float scalar)
    {
      global::System.Console.WriteLine("Member: C.extension(TestClass).operator *(TestClass, float)");
      global::System.Console.WriteLine("Type: C");
      return vector;
    }
    public void Method([TheAspect] int x)
    {
      global::System.Console.WriteLine("Member: C.extension(TestClass).Method(int)");
      global::System.Console.WriteLine("Type: C");
      global::System.Console.WriteLine(test);
      Console.WriteLine("Original.");
    }
    public static void StaticMethod([TheAspect] int x)
    {
      global::System.Console.WriteLine("Member: C.extension(TestClass).StaticMethod(int)");
      global::System.Console.WriteLine("Type: C");
      Console.WriteLine("Original.");
    }
    [TheAspect]
    public int Property
    {
      get
      {
        Console.WriteLine("Original.");
        return 42;
      }
      set
      {
        global::System.Console.WriteLine("Member: Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract.C.extension(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract.TestClass).Property");
        global::System.Console.WriteLine("Type: C");
        global::System.Console.WriteLine(test);
        Console.WriteLine("Original.");
      }
    }
    [TheAspect]
    public static int StaticProperty
    {
      get
      {
        Console.WriteLine("Original.");
        return 42;
      }
      set
      {
        global::System.Console.WriteLine("Member: Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract.C.extension(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract.TestClass).StaticProperty");
        global::System.Console.WriteLine("Type: C");
        Console.WriteLine("Original.");
      }
    }
  }
}
internal class Test
{
  public void Foo()
  {
    var test = new TestClass();
    test.Method(1);
    TestClass.StaticMethod(1);
    test.ClassicStaticExtensionMethod();
    test = test * 5;
    test *= 10;
  }
}