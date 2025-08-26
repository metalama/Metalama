internal class TargetClass
{
  private static readonly global::System.Action<global::System.EventHandler, global::System.Object, (global::System.Object? , global::System.EventArgs)> EventInvokeDelegate_0 = static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.EventFields.RaiseAddRemove.TargetClass)me).Event_Override_Invoke(handler, args);
  private static readonly global::System.Func<global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>, global::System.EventHandler> EventCastDelegate_0 = static b => (owner, args) => b.Invoke((owner, args));
  private event EventHandler _event = default !;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      if (this._eventBroker == null)
      {
        var newBroker = new global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>(this, EventInvokeDelegate_0, EventCastDelegate_0);
        while (null != System.Threading.Interlocked.CompareExchange(ref this._eventBroker, newBroker, null))
          ;
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
      global::System.Console.WriteLine("Add");
      this._event += value;
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
      this._event -= value;
    }
  }
  private void Event_Override_Invoke(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
}