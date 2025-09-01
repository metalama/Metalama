internal class TargetClass
{
  private static readonly global::System.Func<global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>, global::System.EventHandler> EventHandlerCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly global::System.Action<global::System.EventHandler, global::System.Object, (global::System.Object? , global::System.EventArgs)> Event1InvokeDelegate_0 = static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_TwoSimilarEvents.TargetClass)me).Event1_Override_Invoke(handler, args);
  private static readonly global::System.Action<global::System.EventHandler, global::System.Object, (global::System.Object? , global::System.EventArgs)> Event2InvokeDelegate_0 = static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_TwoSimilarEvents.TargetClass)me).Event2_Override_Invoke(handler, args);
  private EventHandler? _handler1;
  private EventHandler? _handler2;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _event1Broker;
  [Override]
  public event EventHandler Event1
  {
    add
    {
      if (this._event1Broker == null)
      {
        global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.InitializeField(ref this._event1Broker, this, Event1InvokeDelegate_0, EventHandlerCastDelegate_0);
      }
      if (this._event1Broker.AddHandler(value))
      {
        this.Event1_Override += this._event1Broker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._event1Broker != null && this._event1Broker.RemoveHandler(value))
      {
        this.Event1_Override -= this._event1Broker.InvocationDelegate;
      }
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
  private void Event1_Override_Invoke(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _event2Broker;
  [Override]
  public event EventHandler Event2
  {
    add
    {
      if (this._event2Broker == null)
      {
        global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.InitializeField(ref this._event2Broker, this, Event2InvokeDelegate_0, EventHandlerCastDelegate_0);
      }
      if (this._event2Broker.AddHandler(value))
      {
        this.Event2_Override += this._event2Broker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._event2Broker != null && this._event2Broker.RemoveHandler(value))
      {
        this.Event2_Override -= this._event2Broker.InvocationDelegate;
      }
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
  private void Event2_Override_Invoke(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
}