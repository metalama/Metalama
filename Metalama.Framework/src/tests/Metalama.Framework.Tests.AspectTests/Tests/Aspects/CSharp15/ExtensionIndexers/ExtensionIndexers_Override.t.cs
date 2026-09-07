[TheAspect]
internal static class C
{
  extension(TestClass test)
  {
    public int this[int index]
    {
      get
      {
        global::System.Console.WriteLine("Member: C.extension(TestClass).this[int].get");
        global::System.Console.WriteLine(test);
        Console.WriteLine("Original.");
        return index;
      }
      set
      {
        global::System.Console.WriteLine("Member: C.extension(TestClass).this[int].set");
        global::System.Console.WriteLine(test);
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
    test[1] += 1;
  }
}