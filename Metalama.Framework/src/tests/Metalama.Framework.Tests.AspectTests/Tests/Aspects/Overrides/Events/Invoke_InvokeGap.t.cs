internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_InvokeGap.TargetClass> EventAdapter_0 = new(static (global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_InvokeGap.TargetClass me) => me.Event_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event_Override += handler, static (handler, me) => me.Event_Override -= handler);
  private EventHandler? _handler;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_InvokeGap.TargetClass>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("Add 1");
      this.Event_OverrideBrokered += value;
    }
    remove
    {
      global::System.Console.WriteLine("Remove 1");
      this.Event_OverrideBrokered -= value;
    }
  }
  private event global::System.EventHandler Event_Override
  {
    add
    {
      global::System.Console.WriteLine("Add 0");
      this._handler = value;
    }
    remove
    {
      global::System.Console.WriteLine("Remove 0");
      this._handler = null;
    }
  }
  public event global::System.EventHandler Event_OverrideBrokered
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
  private void Event_Invoke_Override(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke 0");
    handler.Invoke(args.sender, args.e);
  }
}