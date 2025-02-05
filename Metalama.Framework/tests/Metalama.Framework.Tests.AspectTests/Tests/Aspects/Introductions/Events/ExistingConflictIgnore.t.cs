[Introduction]
internal class TargetClass
{
  public event EventHandler ExistingEvent
  {
    add
    {
      Console.WriteLine("This is original event.");
    }
    remove
    {
      Console.WriteLine("This is original event.");
    }
  }
  public static event EventHandler ExistingEvent_Static
  {
    add
    {
      Console.WriteLine("This is original event.");
    }
    remove
    {
      Console.WriteLine("This is original event.");
    }
  }
}