internal class TargetClass
{
  private static readonly DelegateEventAdapter<EventHandler, (object? , EventArgs), TargetClass> EventBrokerCallbacks_0 = new(static (handler, ref args, me) => me.Event_Invoke_Override(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event_Override += handler, static (handler, me) => me.Event_Override -= handler);
  private event EventHandler _event = default !;
  private volatile EventBroker<EventHandler, (object? , EventArgs), TargetClass>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      EventBroker.EnsureInitialized(ref this._eventBroker, EventBrokerCallbacks_0, this);
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