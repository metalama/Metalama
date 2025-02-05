[Override]
[Introduction]
internal class TargetClass
{
  private event EventHandler? _existingEvent;
  public event EventHandler? ExistingEvent
  {
    add
    {
      global::System.Console.WriteLine("Override");
      this._existingEvent += value;
      return;
    }
    remove
    {
      global::System.Console.WriteLine("Override");
      this._existingEvent -= value;
      return;
    }
  }
  private event EventHandler? _existingEvent_Initializer = Foo;
  public event EventHandler? ExistingEvent_Initializer
  {
    add
    {
      global::System.Console.WriteLine("Override");
      this._existingEvent_Initializer += value;
      return;
    }
    remove
    {
      global::System.Console.WriteLine("Override");
      this._existingEvent_Initializer -= value;
      return;
    }
  }
  public static void Foo(object? sender, EventArgs args)
  {
  }
  public static void Bar(global::System.Object? sender, global::System.EventArgs args)
  {
  }
  private event global::System.EventHandler? _introducedEvent;
  public event global::System.EventHandler? IntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("Override");
      this._introducedEvent += value;
      return;
    }
    remove
    {
      global::System.Console.WriteLine("Override");
      this._introducedEvent -= value;
      return;
    }
  }
  private event global::System.EventHandler? _introducedEvent_Initializer = (global::System.EventHandler? )Bar;
  public event global::System.EventHandler? IntroducedEvent_Initializer
  {
    add
    {
      global::System.Console.WriteLine("Override");
      this._introducedEvent_Initializer += value;
      return;
    }
    remove
    {
      global::System.Console.WriteLine("Override");
      this._introducedEvent_Initializer -= value;
      return;
    }
  }
}