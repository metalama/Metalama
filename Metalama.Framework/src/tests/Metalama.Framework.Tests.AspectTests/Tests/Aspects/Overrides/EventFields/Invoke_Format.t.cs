internal class TargetClass
{
  private static readonly ActionEventBrokerCallbacks<EventHandler, TargetClass, (object? , EventArgs)> EventBrokerCallbacks_0 = new(static (EventHandler handler, TargetClass me, in (object? sender, EventArgs e) args) => me.Event_Invoke_Override(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event_Override += handler, static (handler, me) => me.Event_Override -= handler);
  private event EventHandler _event = default !;
  private volatile ActionEventBroker<EventHandler, TargetClass, (object? , EventArgs)>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      ActionEventBroker<EventHandler, TargetClass, (object? , EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventBrokerCallbacks_0);
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
  private void Event_Invoke_Override(EventHandler handler, (object? sender, EventArgs e) args)
  {
    Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
  }
}