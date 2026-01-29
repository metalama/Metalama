[TheAspect]
internal static class C
{
  extension(int test)
  {
    public global::System.Int32 SomeProperty
    {
      get
      {
        return default;
      }
      set
      {
        global::System.Console.WriteLine("write");
      }
    }
    public static global::System.Int32 SomeStaticProperty
    {
      get
      {
        return default;
      }
      set
      {
        global::System.Console.WriteLine("write");
      }
    }
  }
}