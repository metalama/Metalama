internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.EventBrokerCallbacks<global::System.EventHandler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.InvokeAddRemove.TargetClass, (global::System.Object? , global::System.EventArgs)> EventBrokerCallbacks_0 = new(static (global::System.EventHandler handler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.InvokeAddRemove.TargetClass me, ref (global::System.Object? sender, global::System.EventArgs e) args) => me.Event_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event_Override += handler, static (handler, me) => me.Event_Override -= handler);
  private event EventHandler _event = default !;
  private volatile global::Metalama.Framework.RunTime.EventBroker<global::System.EventHandler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.InvokeAddRemove.TargetClass, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      global::Metalama.Framework.RunTime.EventBroker<global::System.EventHandler, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.InvokeAddRemove.TargetClass, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventBrokerCallbacks_0);
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
      global::System.Console.WriteLine("Add");
      this._event += value;
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
      this._event -= value;
    }
  }
  private void Event_Invoke_Override(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
}