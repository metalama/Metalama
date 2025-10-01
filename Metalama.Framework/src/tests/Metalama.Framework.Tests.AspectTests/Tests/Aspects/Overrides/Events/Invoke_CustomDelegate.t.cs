internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler, (global::System.Object, global::System.Int32, global::System.Int32)> EventBrokerCallbacks_0 = new global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler, (global::System.Object, global::System.Int32, global::System.Int32)>(static (handler, me, args) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass)me).Event_Invoke_Override(handler, args), static b => (sender, args1, arg2) => b.Invoke((sender, args1, arg2)), static (handler, me) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass)me).Event_Override += handler, static (handler, me) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass)me).Event_Override -= handler);
  private MyEventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler, (global::System.Object, global::System.Int32, global::System.Int32)>? _eventBroker;
  [Override]
  public event MyEventHandler Event
  {
    add
    {
      global::Metalama.Framework.RunTime.ActionEventBroker<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler, (global::System.Object, global::System.Int32, global::System.Int32)>.EnsureInitialized(ref this._eventBroker, this, EventBrokerCallbacks_0);
      this._eventBroker.AddHandler(value);
    }
    remove
    {
      this._eventBroker?.RemoveHandler(value);
    }
  }
  private event global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler Event_Override
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
  private void Event_Invoke_Override(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler handler, (global::System.Object sender, global::System.Int32 args1, global::System.Int32 arg2) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.args1, args.arg2);
  }
}