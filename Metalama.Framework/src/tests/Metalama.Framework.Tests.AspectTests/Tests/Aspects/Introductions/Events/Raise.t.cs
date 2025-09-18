[Introduction]
internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)> EventFromAccessorsDelegateSet_0 = new global::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>(static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.RaiseHandler.TargetClass)me).EventFromAccessors_Raise_Introduction(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.RaiseHandler.TargetClass)me).EventFromAccessors_Introduction += handler, static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.RaiseHandler.TargetClass)me).EventFromAccessors_Introduction -= handler);
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
  private void EventFromAccessors_Raise_Introduction(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventFromAccessorsBroker;
  public event global::System.EventHandler EventFromAccessors
  {
    add
    {
      global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._eventFromAccessorsBroker, this, EventFromAccessorsDelegateSet_0);
      this._eventFromAccessorsBroker.AddHandler(value);
    }
    remove
    {
      this._eventFromAccessorsBroker?.RemoveHandler(value);
    }
  }
}