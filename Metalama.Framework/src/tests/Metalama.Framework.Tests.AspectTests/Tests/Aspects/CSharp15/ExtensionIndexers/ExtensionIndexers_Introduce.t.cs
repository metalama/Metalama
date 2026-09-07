[TheAspect]
internal static class C
{
  extension(int test)
  {
    public global::System.String this[global::System.Int32 index]
    {
      get
      {
        global::System.Console.WriteLine("Get.");
        return (global::System.String)index.ToString();
      }
      set
      {
        global::System.Console.WriteLine("Set.");
      }
    }
  }
}