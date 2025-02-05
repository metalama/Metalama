internal class TargetClass
{
  [Override]
  public event EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("This is the overridden accessor.");
    }
    remove
    {
      global::System.Console.WriteLine("This is the overridden accessor.");
    }
  }
}