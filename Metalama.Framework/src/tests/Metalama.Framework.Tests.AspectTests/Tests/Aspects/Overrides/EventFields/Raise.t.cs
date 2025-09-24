internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)> EventDelegateSet_0 = new global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>(static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.EventFields.Raise.TargetClass)me).Event_Raise_Override(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.EventFields.Raise.TargetClass)me).Event_Override += handler, static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.EventFields.Raise.TargetClass)me).Event_Override -= handler);
  private event EventHandler _event = default !;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  [Override]
  public event EventHandler Event
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
  private void Event_Raise_Override(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Raise");
    handler.Invoke(args.sender, args.e);
  }
}