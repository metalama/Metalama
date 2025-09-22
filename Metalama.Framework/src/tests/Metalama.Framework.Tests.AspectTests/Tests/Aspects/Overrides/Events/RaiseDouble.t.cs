    internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)> EventDelegateSet_0 = new global::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>(static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.RaiseDouble.TargetClass)me).Event_Raise_Override(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.RaiseDouble.TargetClass)me).Event_Override += handler, static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.RaiseDouble.TargetClass)me).Event_Override -= handler);
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)> EventDelegateSet_2 = new global::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>(static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.RaiseDouble.TargetClass)me).Event_Raise_Override1(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.RaiseDouble.TargetClass)me).Event_Override1 += handler, static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.RaiseDouble.TargetClass)me).Event_Override1 -= handler);
  private EventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventBroker_1;
  [Override]
  public event EventHandler Event
  {
    add
    {
      global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._eventBroker_1, this, EventDelegateSet_2);
      this._eventBroker_1.AddHandler(value);
    }
    remove
    {
      this._eventBroker_1?.RemoveHandler(value);
    }
  }
  private event global::System.EventHandler Event_Override
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
  public event global::System.EventHandler _eventBrokered
  {
    add
    {
      global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventDelegateSet_0);
      this._eventBroker.AddHandler(value);
    }
    remove
    {
      this._eventBroker?.RemoveHandler(value);
    }
  }
  private void Event_Raise_Override(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke1");
    handler.Invoke(args.sender, args.e);
  }
  private event global::System.EventHandler Event_Override1
  {
    add
    {
      this._eventBrokered += value;
    }
    remove
    {
      this._eventBrokered -= value;
    }
  }
  private void Event_Raise_Override1(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke2");
    handler.Invoke(args.sender, args.e);
  }
}