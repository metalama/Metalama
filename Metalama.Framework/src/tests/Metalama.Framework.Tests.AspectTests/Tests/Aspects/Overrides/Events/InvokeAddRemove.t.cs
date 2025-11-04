internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.InvokeAddRemove.TargetClass> EventAdapter_0 = new(static (global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args, global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.InvokeAddRemove.TargetClass me) => me.Event_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event_Override += handler, static (handler, me) => me.Event_Override -= handler);
  private EventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.InvokeAddRemove.TargetClass>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref this._eventBroker, EventAdapter_0, this);
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
      global::System.Console.WriteLine("Add");
      this._handler = value;
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
      this._handler = null;
    }
  }
  private void Event_Invoke_Override(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
}