[Introduction]
internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.Invoke.TargetClass> EventFromAccessorsBrokerCallbacks_0 = new(static (handler, ref args, me) => me.EventFromAccessors_Invoke_Introduction(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.EventFromAccessors_Introduction += handler, static (handler, me) => me.EventFromAccessors_Introduction -= handler);
  private event global::System.EventHandler EventFromAccessors_Introduction
  {
    add
    {
      global::System.Console.WriteLine("Add");
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
    }
  }
  private void EventFromAccessors_Invoke_Introduction(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.Invoke.TargetClass>? _eventFromAccessorsBroker;
  public event global::System.EventHandler EventFromAccessors
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref this._eventFromAccessorsBroker, EventFromAccessorsBrokerCallbacks_0, this);
      this._eventFromAccessorsBroker.AddHandler(value);
    }
    remove
    {
      this._eventFromAccessorsBroker?.RemoveHandler(value);
    }
  }
}