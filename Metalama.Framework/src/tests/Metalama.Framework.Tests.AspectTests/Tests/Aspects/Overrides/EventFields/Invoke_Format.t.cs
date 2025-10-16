internal class TargetClass
{
  private static readonly ActionEventBrokerCallbacks<EventHandler, (object? , EventArgs)> EventBrokerCallbacks_0 = new ActionEventBrokerCallbacks<EventHandler, (object? , EventArgs)>(static (handler, me, args) => ((TargetClass)me).Event_Invoke_Override(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((TargetClass)me).Event_Override += handler, static (handler, me) => ((TargetClass)me).Event_Override -= handler);
  private event EventHandler _event = default !;
  private volatile ActionEventBroker<EventHandler, (object? , EventArgs)>? _eventBroker;
  [Override]
  public event EventHandler Event
  {
    add
    {
      ActionEventBroker<EventHandler, (object? , EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventBrokerCallbacks_0);
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