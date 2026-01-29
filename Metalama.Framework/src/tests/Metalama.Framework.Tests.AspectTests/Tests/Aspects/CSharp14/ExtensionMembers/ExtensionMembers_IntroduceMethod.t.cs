[TheAspect]
internal static class C
{
  extension(int test)
  {
    public global::System.Int32 SomeMethod(global::System.Int32 a, global::System.Int32 b)
    {
      global::System.Console.WriteLine("Member: C.extension(int).SomeMethod(int, int)");
      global::System.Console.WriteLine("Type: C");
      return (global::System.Int32)(test + a + b);
    }
    public static global::System.Int32 SomeStaticMethod(global::System.Int32 a, global::System.Int32 b)
    {
      global::System.Console.WriteLine("Member: C.extension(int).SomeStaticMethod(int, int)");
      global::System.Console.WriteLine("Type: C");
      return (global::System.Int32)(a + b);
    }
    public static global::System.Collections.Generic.IEnumerable<global::System.Int32> operator +(global::System.Collections.Generic.IEnumerable<global::System.Int32> a, global::System.Int32 b)
    {
      global::System.Console.WriteLine("Member: C.extension(int).operator +(IEnumerable<int>, int)");
      global::System.Console.WriteLine("Type: C");
      return default;
    }
  }
}