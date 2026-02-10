// Warning MY001 on `AccessorEvent`: `Event 'AccessorEvent': HasRaiseMethod=False, CanRaise=False`
internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_AccessorEvent.TargetClass> AccessorEventAdapter_0 = new(static (global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_AccessorEvent.TargetClass me) => me.AccessorEvent_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.AccessorEvent_Override += handler, static (handler, me) => me.AccessorEvent_Override -= handler);
  private EventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_AccessorEvent.TargetClass>? _accessorEventBroker;
  [Override]
  public event EventHandler AccessorEvent
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref this._accessorEventBroker, AccessorEventAdapter_0, this);
      this._accessorEventBroker.AddHandler(value);
    }
    remove
    {
      this._accessorEventBroker?.RemoveHandler(value);
    }
  }
  private event global::System.EventHandler AccessorEvent_Override
  {
    add
    {
      this._handler = value;
    }
    remove
    {
      this._handler = null;
    }
  }
  private void AccessorEvent_Invoke_Override(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke override");
    handler.Invoke(args.sender, args.e);
  }
}
