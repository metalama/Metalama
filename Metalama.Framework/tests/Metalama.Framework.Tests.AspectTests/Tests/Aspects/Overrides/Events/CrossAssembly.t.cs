[Override]
[Introduction]
internal class TargetClass
{
  public event EventHandler ExistingEvent
  {
    add
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      return;
    }
    remove
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      return;
    }
  }
  public event global::System.EventHandler IntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("Override");
      global::System.Console.WriteLine("Original");
      return;
    }
    remove
    {
      global::System.Console.WriteLine("Override");
      global::System.Console.WriteLine("Original");
      return;
    }
  }
}