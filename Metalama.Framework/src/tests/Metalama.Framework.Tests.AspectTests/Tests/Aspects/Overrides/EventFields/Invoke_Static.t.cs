internal static class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.None> EventBrokerCallbacks_0 = new(static (handler, ref args, _) => Event_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, _) => Event_Override += handler, static (handler, _) => Event_Override -= handler);
  private static event EventHandler _event = default !;
  private static volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.None>? _eventBroker;
  [Override]
  public static event EventHandler Event
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref _eventBroker, EventBrokerCallbacks_0);
      _eventBroker.AddHandler(value);
    }
    remove
    {
      _eventBroker?.RemoveHandler(value);
    }
  }
  private static event global::System.EventHandler Event_Override
  {
    add
    {
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke_Static.TargetClass._event += value;
    }
    remove
    {
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke_Static.TargetClass._event -= value;
    }
  }
  private static void Event_Invoke_Override(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
}