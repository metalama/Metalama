internal static class C
{
  extension(TestClass test)
  {
    [TheAspect]
    public int Property
    {
      get
      {
        global::System.Console.WriteLine("Member: C.extension(TestClass).Property.get");
        global::System.Console.WriteLine("Type: C");
        global::System.Console.WriteLine(test);
        Console.WriteLine("Original.");
        return 42;
      }
      set
      {
        global::System.Console.WriteLine("Member: C.extension(TestClass).Property.set");
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
        global::System.Console.WriteLine("Member: C.extension(TestClass).StaticProperty.get");
        global::System.Console.WriteLine("Type: C");
        Console.WriteLine("Original.");
        return 42;
      }
      set
      {
        global::System.Console.WriteLine("Member: C.extension(TestClass).StaticProperty.set");
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
    test.Property += 1;
    TestClass.StaticProperty += 1;
  }
}