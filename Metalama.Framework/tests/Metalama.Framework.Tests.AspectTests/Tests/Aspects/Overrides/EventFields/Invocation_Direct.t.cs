[Override]
[Introduction]
internal class TargetClass
{
  private event EventHandler? _event;
  public event EventHandler? Event
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._event += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._event -= value;
    }
  }
  private static event EventHandler? _staticEvent;
  public static event EventHandler? StaticEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Direct.TargetClass._staticEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Direct.TargetClass._staticEvent -= value;
    }
  }
  public void Foo()
  {
    if (_event != null)
    {
      _event(this, new EventArgs());
    }
    if (_staticEvent != null)
    {
      _staticEvent(this, new EventArgs());
    }
  }
  public void Bar()
  {
    if (this._introducedEvent != null)
    {
      this._introducedEvent(this, new global::System.EventArgs());
    }
    if (global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Direct.TargetClass._introducedStaticEvent != null)
    {
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Direct.TargetClass._introducedStaticEvent(this, new global::System.EventArgs());
    }
  }
  private event global::System.EventHandler? _introducedEvent;
  public event global::System.EventHandler? IntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._introducedEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._introducedEvent -= value;
    }
  }
  private static event global::System.EventHandler? _introducedStaticEvent;
  public static event global::System.EventHandler? IntroducedStaticEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Direct.TargetClass._introducedStaticEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Direct.TargetClass._introducedStaticEvent -= value;
    }
  }
}