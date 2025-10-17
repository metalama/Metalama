internal class TargetClass
{
  private static readonly DelegateEventAdapter<EventHandler, TargetClass, (object? , EventArgs)> EventBrokerCallbacks_0 = new(static (EventHandler handler, TargetClass me, ref (object? sender, EventArgs e) args) => me.Event_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event_Override += handler, static (handler, me) => me.Event_Override -= handler);
  private event EventHandler _event = default !;
  private volatile EventBroker<EventHandler, TargetClass, (object? , EventArgs)>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      EventBroker<EventHandler, TargetClass, (object? , EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventBrokerCallbacks_0);
      this._eventBroker.AddHandler(value);
    }
    remove
    {
      this._eventBroker?.RemoveHandler(value);
    }
  }
  private event EventHandler Event_Override
  {
    add
    {
      this._event += value;
    }
    remove
    {
      this._event -= value;
    }
  }
  private void Event_Invoke_Override(EventHandler handler, ref (object? sender, EventArgs e) args)
  {
    Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
}