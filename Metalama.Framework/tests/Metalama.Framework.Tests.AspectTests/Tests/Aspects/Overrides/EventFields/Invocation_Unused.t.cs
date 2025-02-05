[Override]
[Introduction]
internal class TargetClass
{
  public event EventHandler? Event
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
    }
  }
  public static event EventHandler? StaticEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
    }
  }
  public event global::System.EventHandler? IntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
    }
  }
  public static event global::System.EventHandler? IntroducedStaticEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
    }
  }
}