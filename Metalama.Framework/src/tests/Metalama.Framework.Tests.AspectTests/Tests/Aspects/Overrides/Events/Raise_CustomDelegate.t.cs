internal class TargetClass
{
  private static readonly global::System.Func<global::Metalama.Framework.RunTime.ActionEventBroker<global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_CustomDelegate.MyEventHandler, (global::System.Object, global::System.Int32, global::System.Int32)>, global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_CustomDelegate.MyEventHandler> EventCastDelegate_0 = static b => (sender, args1, arg2) => b.Invoke((sender, args1, arg2));
  private static readonly global::System.Action<global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_CustomDelegate.MyEventHandler, global::System.Object, (global::System.Object, global::System.Int32, global::System.Int32)> EventInvokeDelegate_0 = static (handler, me, args) => ((global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_CustomDelegate.TargetClass)me).Event_Override_Invoke(handler, args);
  private MyEventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_CustomDelegate.MyEventHandler, (global::System.Object, global::System.Int32, global::System.Int32)>? _eventBroker;
  [Override]
  public event MyEventHandler Event
  {
    add
    {
      if (this._eventBroker == null)
      {
        global::Metalama.Framework.RunTime.ActionEventBroker<global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_CustomDelegate.MyEventHandler, (global::System.Object, global::System.Int32, global::System.Int32)>.InitializeField(ref this._eventBroker, this, EventInvokeDelegate_0, EventCastDelegate_0);
      }
      if (this._eventBroker.AddHandler(value))
      {
        this.Event_Override += this._eventBroker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._eventBroker != null && this._eventBroker.RemoveHandler(value))
      {
        this.Event_Override -= this._eventBroker.InvocationDelegate;
      }
    }
  }
  private event global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_CustomDelegate.MyEventHandler Event_Override
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
  private void Event_Override_Invoke(global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_CustomDelegate.MyEventHandler handler, (global::System.Object sender, global::System.Int32 args1, global::System.Int32 arg2) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.args1, args.arg2);
  }
}