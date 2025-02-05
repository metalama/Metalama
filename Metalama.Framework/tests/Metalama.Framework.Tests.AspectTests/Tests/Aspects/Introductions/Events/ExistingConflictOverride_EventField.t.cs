[Introduction]
internal class TargetClass : BaseClass
{
  public event EventHandler? ExistingEvent
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
  public static event EventHandler? ExistingEvent_Static
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
  public override event global::System.EventHandler? ExistingBaseEvent;
  public event global::System.EventHandler? NotExistingEvent;
  public static event global::System.EventHandler? NotExistingEvent_Static;
}