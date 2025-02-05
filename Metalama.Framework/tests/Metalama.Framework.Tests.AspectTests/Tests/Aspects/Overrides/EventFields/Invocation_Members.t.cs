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
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members.TargetClass._staticEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members.TargetClass._staticEvent -= value;
    }
  }
  public void Foo()
  {
    _event?.Invoke(this, new EventArgs());
    _ = _event?.GetInvocationList();
    _ = _event?.BeginInvoke(this, new EventArgs(), x =>
    {
    }, this);
    _ = _event?.Method;
    _ = _event?.Target;
    _staticEvent?.Invoke(this, new EventArgs());
    _ = _staticEvent?.GetInvocationList();
    _ = _staticEvent?.BeginInvoke(this, new EventArgs(), x =>
    {
    }, this);
    _ = _staticEvent?.Method;
    _ = _staticEvent?.Target;
  }
  public void Bar()
  {
    this._introducedEvent?.Invoke(this, new global::System.EventArgs());
    var a = this._introducedEvent?.GetInvocationList();
    var b = this._introducedEvent?.BeginInvoke(this, new global::System.EventArgs(), new global::System.AsyncCallback(Callback), this);
    var c = this._introducedEvent?.Method;
    var d = this._introducedEvent?.Target;
    global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members.TargetClass._introducedStaticEvent?.Invoke(this, new global::System.EventArgs());
    var e = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members.TargetClass._introducedStaticEvent?.GetInvocationList();
    var f = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members.TargetClass._introducedStaticEvent?.BeginInvoke(null, new global::System.EventArgs(), new global::System.AsyncCallback(Callback), null);
    var g = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members.TargetClass._introducedStaticEvent?.Method;
    var h = global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members.TargetClass._introducedStaticEvent?.Target;
  }
  private void Callback(global::System.IAsyncResult result)
  {
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
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members.TargetClass._introducedStaticEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members.TargetClass._introducedStaticEvent -= value;
    }
  }
}