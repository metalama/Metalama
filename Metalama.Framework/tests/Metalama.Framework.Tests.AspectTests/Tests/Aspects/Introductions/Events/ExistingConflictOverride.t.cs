[Introduction]
internal class TargetClass : BaseClass
{
  public event EventHandler ExistingEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      Console.WriteLine("This is original event.");
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      Console.WriteLine("This is original event.");
    }
  }
  public static event EventHandler ExistingEvent_Static
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      Console.WriteLine("This is original event.");
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      Console.WriteLine("This is original event.");
    }
  }
  public override event global::System.EventHandler ExistingBaseEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.ExistingBaseEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
      base.ExistingBaseEvent -= value;
    }
  }
  public event global::System.EventHandler NotExistingEvent
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
    }
  }
  public static event global::System.EventHandler NotExistingEvent_Static
  {
    add
    {
      global::System.Console.WriteLine("This is introduced event.");
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced event.");
    }
  }
}