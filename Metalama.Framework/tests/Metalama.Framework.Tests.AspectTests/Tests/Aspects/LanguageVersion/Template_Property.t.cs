[TheAspect]
internal class Target
{
  public global::System.String Property1
  {
    get
    {
      return (global::System.String)"""get""";
    }
  }
  public global::System.String Property2
  {
    get
    {
      return (global::System.String)"";
    }
    set
    {
      global::System.Console.WriteLine("""set""");
    }
  }
}