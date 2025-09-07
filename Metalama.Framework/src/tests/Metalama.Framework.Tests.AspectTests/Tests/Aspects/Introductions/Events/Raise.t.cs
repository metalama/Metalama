[Introduction]
internal class TargetClass
{
  private static readonly global::System.Func<global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>, global::System.EventHandler> EventHandlerCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly global::System.Action<global::System.EventHandler, global::System.Object, (global::System.Object? , global::System.EventArgs)> EventFromAccessorsInvokeDelegate_0 = static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.RaiseHandler.TargetClass)me).EventFromAccessors_Introduction_Invoke(handler, args);
  private event global::System.EventHandler EventFromAccessors_Introduction
  {
    add
    {
      global::System.Console.WriteLine("Add");
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
    }
  }
  private void EventFromAccessors_Introduction_Invoke(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventFromAccessorsBroker;
  public event global::System.EventHandler EventFromAccessors
  {
    add
    {
      if (this._eventFromAccessorsBroker == null)
      {
        global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.InitializeField(ref this._eventFromAccessorsBroker, this, EventFromAccessorsInvokeDelegate_0, EventHandlerCastDelegate_0);
      }
      if (this._eventFromAccessorsBroker.AddHandler(value))
      {
        this.EventFromAccessors_Introduction += this._eventFromAccessorsBroker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._eventFromAccessorsBroker != null && this._eventFromAccessorsBroker.RemoveHandler(value))
      {
        this.EventFromAccessors_Introduction -= this._eventFromAccessorsBroker.InvocationDelegate;
      }
    }
  }
}