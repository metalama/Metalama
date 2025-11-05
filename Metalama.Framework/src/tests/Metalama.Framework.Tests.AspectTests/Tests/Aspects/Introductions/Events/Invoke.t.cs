[Introduction]
internal class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.Invoke.TargetClass> EventFromAccessorsAdapter_0 = new(static (global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.Invoke.TargetClass me) => me.EventFromAccessors_Invoke_Introduction(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.EventFromAccessors_Introduction += handler, static (handler, me) => me.EventFromAccessors_Introduction -= handler);
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.None> StaticEventFromAccessorsAdapter_0 = new(static (global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args, global::Metalama.Framework.None _) => StaticEventFromAccessors_Invoke_Introduction(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, _) => StaticEventFromAccessors_Introduction += handler, static (handler, _) => StaticEventFromAccessors_Introduction -= handler);
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
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref this._eventFromAccessorsBroker, EventFromAccessorsAdapter_0, this);
      this._eventFromAccessorsBroker.AddHandler(value);
    }
    remove
    {
      this._eventFromAccessorsBroker?.RemoveHandler(value);
    }
  }
  private static event global::System.EventHandler StaticEventFromAccessors_Introduction
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
  private static void StaticEventFromAccessors_Invoke_Introduction(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
  private static volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.None>? _staticEventFromAccessorsBroker;
  public static event global::System.EventHandler StaticEventFromAccessors
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref _staticEventFromAccessorsBroker, StaticEventFromAccessorsAdapter_0);
      _staticEventFromAccessorsBroker.AddHandler(value);
    }
    remove
    {
      _staticEventFromAccessorsBroker?.RemoveHandler(value);
    }
  }
}