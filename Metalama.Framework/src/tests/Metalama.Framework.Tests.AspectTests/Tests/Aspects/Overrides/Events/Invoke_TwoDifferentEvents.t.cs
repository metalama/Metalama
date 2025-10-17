internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_TwoDifferentEvents.TargetClass> Event1BrokerCallbacks_0 = new(static (global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_TwoDifferentEvents.TargetClass me) => me.Event1_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event1_Override += handler, static (handler, me) => me.Event1_Override -= handler);
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler<global::System.EventArgs>, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_TwoDifferentEvents.TargetClass> Event2BrokerCallbacks_0 = new(static (global::System.EventHandler<global::System.EventArgs> handler, ref (global::System.Object? sender, global::System.EventArgs e) args, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_TwoDifferentEvents.TargetClass me) => me.Event2_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event2_Override += handler, static (handler, me) => me.Event2_Override -= handler);
  private EventHandler? _handler1;
  private EventHandler<EventArgs>? _handler2;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_TwoDifferentEvents.TargetClass>? _event1Broker;
  [Override]
  public event EventHandler Event1
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref this._event1Broker, Event1BrokerCallbacks_0, this);
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
  private void Event1_Invoke_Override(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler<global::System.EventArgs>, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_TwoDifferentEvents.TargetClass>? _event2Broker;
  [Override]
  public event EventHandler<EventArgs> Event2
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref this._event2Broker, Event2BrokerCallbacks_0, this);
      this._event2Broker.AddHandler(value);
    }
    remove
    {
      this._event2Broker?.RemoveHandler(value);
    }
  }
  private event global::System.EventHandler<global::System.EventArgs> Event2_Override
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
  private void Event2_Invoke_Override(global::System.EventHandler<global::System.EventArgs> handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
}