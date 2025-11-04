internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler, (global::System.Object, global::System.Int32, global::System.Int32), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass> EventAdapter_0 = new(static (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler handler, ref (global::System.Object sender, global::System.Int32 args1, global::System.Int32 arg2) args, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass me) => me.Event_Invoke_Override(handler, ref args), static b => (sender, args1, arg2) => b.Invoke((sender, args1, arg2)), static (handler, me) => me.Event_Override += handler, static (handler, me) => me.Event_Override -= handler);
  private MyEventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.MyEventHandler, (global::System.Object, global::System.Int32, global::System.Int32), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_CustomDelegate.TargetClass>? _eventBroker;
  [Override]
  public event MyEventHandler Event
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref this._eventBroker, EventAdapter_0, this);
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