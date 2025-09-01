internal class TargetClass
{
  private static readonly global::System.Func<global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>, global::System.EventHandler> EventHandlerCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly global::System.Action<global::System.EventHandler, global::System.Object, (global::System.Object? , global::System.EventArgs)> EventInvokeDelegate_0 = static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.AsMethod_Raise.TargetClass)me).Event_Override_Invoke(handler, args);
  private EventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      if (this._eventBroker == null)
      {
        global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.InitializeField(ref this._eventBroker, this, EventInvokeDelegate_0, EventHandlerCastDelegate_0);
      }
      if (this._eventBroker.AddHandler(value))
      {
        this.Event_Override += this._eventBroker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._eventBroker != null && this._eventBroker.RemoveHandler(value))
      {
        this.Event_Override -= this._eventBroker.InvocationDelegate;
      }
    }
  }
  private event global::System.EventHandler Event_Override
  {
    add
    {
      this._handler = value;
      Console.WriteLine("OriginalAdd");
    }
    remove
    {
      this._handler = null;
      Console.WriteLine("OriginalRemove");
    }
  }
  private void Event_Override_Invoke(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Overridden invoke");
    handler.Invoke(args.sender, args.e);
  }
}