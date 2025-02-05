[TheAspect]
internal class Target
{
  public event global::System.EventHandler Event1
  {
    add
    {
      global::System.Console.WriteLine("""add""");
    }
    remove
    {
    }
  }
  public event global::System.EventHandler Event2
  {
    add
    {
    }
    remove
    {
      global::System.Console.WriteLine("""remove""");
    }
  }
}