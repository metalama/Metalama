internal class TargetClass
{
  private static readonly global::System.Func<global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler?, (global::System.Object? , global::System.EventArgs)>, global::System.EventHandler?> EventHandlerCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly global::System.Action<global::System.EventHandler?, global::System.Object, (global::System.Object? , global::System.EventArgs)> EventInvokeDelegate_0 = static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.EventFields.AsMethod_Raise.TargetClass)me).Event_Raise_Override(handler, args);
  private static readonly global::System.Action<global::System.EventHandler?, global::System.Object> EventAddDelegate_0 = static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.EventFields.AsMethod_Raise.TargetClass)me).Event_Override += handler;
  private static readonly global::System.Action<global::System.EventHandler?, global::System.Object> EventRemoveDelegate_0 = static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.EventFields.AsMethod_Raise.TargetClass)me).Event_Override -= handler;
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler?, (global::System.Object? , global::System.EventArgs)> EventDelegateSet_0 = newglobal::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler?, (global::System.Object? , global::System.EventArgs)>(EventInvokeDelegate_0, EventHandlerCastDelegate_0, EventAddDelegate_0, EventRemoveDelegate_0);
  private event EventHandler? _event;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler?, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  [Override]
  public event EventHandler? Event
  {
    add
    {
      global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler?, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventDelegateSet_0);
      this._eventBroker.AddHandler(value);
    }
    remove
    {
      this._eventBroker.RemoveHandler(value);
    }
  }
  private event global::System.EventHandler? Event_Override
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
  private void Event_Raise_Override(global::System.EventHandler? handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Overridden raise");
    handler.Invoke(args.sender, args.e);
  }
}