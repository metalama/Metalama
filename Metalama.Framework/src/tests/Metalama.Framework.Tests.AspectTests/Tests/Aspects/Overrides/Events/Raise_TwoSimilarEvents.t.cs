internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)> Event1BrokerCallbacks_0 = new global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>(static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_TwoSimilarEvents.TargetClass)me).Event1_Raise_Override(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_TwoSimilarEvents.TargetClass)me).Event1_Override += handler, static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_TwoSimilarEvents.TargetClass)me).Event1_Override -= handler);
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)> Event2BrokerCallbacks_0 = new global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>(static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_TwoSimilarEvents.TargetClass)me).Event2_Raise_Override(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_TwoSimilarEvents.TargetClass)me).Event2_Override += handler, static (handler, me) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_TwoSimilarEvents.TargetClass)me).Event2_Override -= handler);
  private EventHandler? _handler1;
  private EventHandler? _handler2;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _event1Broker;
  [Override]
  public event EventHandler Event1
  {
    add
    {
      global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._event1Broker, this, Event1BrokerCallbacks_0);
      this._event1Broker.AddHandler(value);
    }
    remove
    {
      this._event1Broker?.RemoveHandler(value);
    }
  }
  private event global::System.EventHandler Event1_Override
  {
    add
    {
      this._handler1 = value;
    }
    remove
    {
      this._handler1 = null;
    }
  }
  private void Event1_Raise_Override(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Raise");
    handler.Invoke(args.sender, args.e);
  }
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _event2Broker;
  [Override]
  public event EventHandler Event2
  {
    add
    {
      global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._event2Broker, this, Event2BrokerCallbacks_0);
      this._event2Broker.AddHandler(value);
    }
    remove
    {
      this._event2Broker?.RemoveHandler(value);
    }
  }
  private event global::System.EventHandler Event2_Override
  {
    add
    {
      this._handler2 = value;
    }
    remove
    {
      this._handler2 = null;
    }
  }
  private void Event2_Raise_Override(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Raise");
    handler.Invoke(args.sender, args.e);
  }
}