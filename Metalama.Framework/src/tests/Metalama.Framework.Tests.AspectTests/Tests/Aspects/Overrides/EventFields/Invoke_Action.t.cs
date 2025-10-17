internal class TargetClass<T>
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.Action, global::System.ValueTuple, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke_Action.TargetClass<T>> Event0BrokerCallbacks_0 = new(static (handler, ref args, me) => me.Event0_Invoke_Override(handler, ref args), static b => () => b.Invoke(global::System.ValueTuple.Create()), static (handler, me) => me.Event0_Override += handler, static (handler, me) => me.Event0_Override -= handler);
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.Action<global::System.Int32>, global::System.ValueTuple<global::System.Int32>, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke_Action.TargetClass<T>> Event1BrokerCallbacks_0 = new(static (handler, ref args, me) => me.Event1_Invoke_Override(handler, ref args), static b => (obj) => b.Invoke(global::System.ValueTuple.Create(obj)), static (handler, me) => me.Event1_Override += handler, static (handler, me) => me.Event1_Override -= handler);
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.Action<T, global::System.Int32>, (T, global::System.Int32), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke_Action.TargetClass<T>> Event2BrokerCallbacks_0 = new(static (handler, ref args, me) => me.Event2_Invoke_Override(handler, ref args), static b => (arg1, arg2) => b.Invoke((arg1, arg2)), static (handler, me) => me.Event2_Override += handler, static (handler, me) => me.Event2_Override -= handler);
  private event Action _event0 = default !;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.Action, global::System.ValueTuple, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke_Action.TargetClass<T>>? _event0Broker;
  [Override]
  public event Action Event0
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref this._event0Broker, Event0BrokerCallbacks_0, this);
      this._event0Broker.AddHandler(value);
    }
    remove
    {
      this._event0Broker?.RemoveHandler(value);
    }
  }
  private event global::System.Action Event0_Override
  {
    add
    {
      this._event0 += value;
    }
    remove
    {
      this._event0 -= value;
    }
  }
  private void Event0_Invoke_Override(global::System.Action handler, ref global::System.ValueTuple args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke();
  }
  private event Action<int> _event1 = default !;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.Action<global::System.Int32>, global::System.ValueTuple<global::System.Int32>, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke_Action.TargetClass<T>>? _event1Broker;
  [Override]
  public event Action<int> Event1
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
  private event global::System.Action<global::System.Int32> Event1_Override
  {
    add
    {
      this._event1 += value;
    }
    remove
    {
      this._event1 -= value;
    }
  }
  private void Event1_Invoke_Override(global::System.Action<global::System.Int32> handler, ref global::System.ValueTuple<global::System.Int32> args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.Item1);
  }
  private event Action<T, int> _event2 = default !;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.Action<T, global::System.Int32>, (T, global::System.Int32), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke_Action.TargetClass<T>>? _event2Broker;
  [Override]
  public event Action<T, int> Event2
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
  private event global::System.Action<T, global::System.Int32> Event2_Override
  {
    add
    {
      this._event2 += value;
    }
    remove
    {
      this._event2 -= value;
    }
  }
  private void Event2_Invoke_Override(global::System.Action<T, global::System.Int32> handler, ref (T arg1, global::System.Int32 arg2) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.arg1, args.arg2);
  }
}