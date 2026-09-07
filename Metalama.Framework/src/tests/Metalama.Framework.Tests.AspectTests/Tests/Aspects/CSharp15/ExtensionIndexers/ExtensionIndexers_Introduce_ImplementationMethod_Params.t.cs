[TheAspect]
internal static class C
{
  extension(int test)
  {
    public global::System.String this[params global::System.Int32[] index]
    {
      get
      {
        global::System.Console.WriteLine("Get.");
        return (global::System.String)index.Length.ToString();
      }
      set
      {
        global::System.Console.WriteLine("Set.");
      }
    }
  }
}