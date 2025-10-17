internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke.TargetClass, (global::System.Object? , global::System.EventArgs)> EventBrokerCallbacks_0 = new(static (handler, me, ref args) => me.Event_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event_Override += handler, static (handler, me) => me.Event_Override -= handler);
  private event EventHandler _event = default !;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke.TargetClass, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke.TargetClass, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventBrokerCallbacks_0);
      this._eventBroker.AddHandler(value);
    }
    remove
    {
      this._eventBroker?.RemoveHandler(value);
    }
  }
  private event global::System.EventHandler Event_Override
  {
    add
    {
      this._event += value;
    }
    remove
    {
      this._event -= value;
    }
  }
  private void Event_Invoke_Override(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
}