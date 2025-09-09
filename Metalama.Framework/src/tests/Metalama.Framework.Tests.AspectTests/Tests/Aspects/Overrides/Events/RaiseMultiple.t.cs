internal class TargetClass
{
  private static readonly global::System.Func<global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>, global::System.EventHandler> EventCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly global::System.Action<global::System.EventHandler, global::System.Object, (global::System.Object? , global::System.EventArgs)> EventInvokeDelegate_0 = static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.RaiseDouble.TargetClass)me).Event_Override_Invoke(handler, args);
  private EventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventBroker_1;
  [Override]
  public event EventHandler Event
  {
    add
    {
      if (this._eventBroker_1 == null)
      {
        global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._eventBroker_1, this, EventInvokeDelegate_0, EventCastDelegate_0);
      }
      if (this._eventBroker_1.AddHandler(value))
      {
        this.Event_Override1 += this._eventBroker_1.InvocationDelegate;
      }
    }
    remove
    {
      if (this._eventBroker_1 != null && this._eventBroker_1.RemoveHandler(value))
      {
        this.Event_Override1 -= this._eventBroker_1.InvocationDelegate;
      }
    }
  }
  private void Event_Override_Invoke(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke1");
    handler.Invoke(args.sender, args.e);
  }
  private event global::System.EventHandler Event_Override1
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
  private void Event_Override1_Invoke(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke2");
    handler.Invoke(args.sender, args.e);
  }
}