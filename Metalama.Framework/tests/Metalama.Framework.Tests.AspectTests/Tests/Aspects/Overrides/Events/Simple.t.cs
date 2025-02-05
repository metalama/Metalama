[Override]
[Introduction]
internal class TargetClass
{
  private HashSet<EventHandler> handlers = new();
  public event EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      Console.WriteLine("This is the original add.");
      handlers.Add(value);
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      Console.WriteLine("This is the original remove.");
      handlers.Remove(value);
    }
  }
  private static HashSet<EventHandler> staticHandlers = new();
  public static event EventHandler StaticEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      Console.WriteLine("This is the original add.");
      staticHandlers.Add(value);
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      Console.WriteLine("This is the original remove.");
      staticHandlers.Remove(value);
    }
  }
  public event global::System.EventHandler IntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      global::System.Console.WriteLine("This is the introduced add.");
      this.handlers.Add(value);
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      global::System.Console.WriteLine("This is the introduced remove.");
      this.handlers.Remove(value);
    }
  }
  public static event global::System.EventHandler IntroducedStaticEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      global::System.Console.WriteLine("This is the introduced add.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Simple.TargetClass.staticHandlers.Add(value);
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      global::System.Console.WriteLine("This is the introduced remove.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Simple.TargetClass.staticHandlers.Remove(value);
    }
  }
}