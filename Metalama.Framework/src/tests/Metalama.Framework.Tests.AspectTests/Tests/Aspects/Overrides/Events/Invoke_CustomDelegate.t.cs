internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.EventBrokerCallbacks<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass, (global::System.Object, global::System.Int32, global::System.Int32)> EventBrokerCallbacks_0 = new(static (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler handler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass me, ref (global::System.Object sender, global::System.Int32 args1, global::System.Int32 arg2) args) => me.Event_Invoke_Override(handler, ref args), static b => (sender, args1, arg2) => b.Invoke((sender, args1, arg2)), static (handler, me) => me.Event_Override += handler, static (handler, me) => me.Event_Override -= handler);
  private MyEventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.EventBroker<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass, (global::System.Object, global::System.Int32, global::System.Int32)>? _eventBroker;
  [Override]
  public event MyEventHandler Event
  {
    add
    {
      global::Metalama.Framework.RunTime.EventBroker<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass, (global::System.Object, global::System.Int32, global::System.Int32)>.EnsureInitialized(ref this._eventBroker, this, EventBrokerCallbacks_0);
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
  private void Event_Invoke_Override(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler handler, ref (global::System.Object sender, global::System.Int32 args1, global::System.Int32 arg2) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.args1, args.arg2);
  }
}