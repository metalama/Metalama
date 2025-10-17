internal class TargetCode
{
  private static readonly DelegateEventAdapter<EventHandler<EventArgs>, (object? , EventArgs), TargetCode> EventFieldBrokerCallbacks_0 = new(static (EventHandler<EventArgs> handler, ref (object? sender, EventArgs e) args, TargetCode me) => me.EventField_Invoke_WeakEvent(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.EventField_WeakEvent += handler, static (handler, me) => me.EventField_WeakEvent -= handler);
  private static readonly DelegateEventAdapter<EventHandler<EventArgs>, (object? , EventArgs), TargetCode> EventBrokerCallbacks_0 = new(static (EventHandler<EventArgs> handler, ref (object? sender, EventArgs e) args, TargetCode me) => me.Event_Invoke_WeakEvent(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.Event_WeakEvent += handler, static (handler, me) => me.Event_WeakEvent -= handler);
  private List<EventHandler<EventArgs>> _delegates = new();
  private event EventHandler<EventArgs> _eventField = default !;
  private volatile EventBroker<EventHandler<EventArgs>, (object? , EventArgs), TargetCode>? _eventFieldBroker;
  [WeakEvent]
  public event EventHandler<EventArgs> EventField
  {
    add
    {
      EventBroker.EnsureInitialized(ref this._eventFieldBroker, EventFieldBrokerCallbacks_0, this);
      this._eventFieldBroker.AddHandler(value);
    }
    remove
    {
      this._eventFieldBroker?.RemoveHandler(value);
    }
  }
  private event EventHandler<EventArgs> EventField_WeakEvent
  {
    add
    {
      weakEventContainerForEventField.AddHandler(value);
    }
    remove
    {
      weakEventContainerForEventField.RemoveHandler(value);
    }
  }
  private void EventField_Invoke_WeakEvent(EventHandler<EventArgs> handler, ref (object? sender, EventArgs e) args)
  {
    weakEventContainerForEventField.Invoke((args.sender, args.e));
  }
  private volatile EventBroker<EventHandler<EventArgs>, (object? , EventArgs), TargetCode>? _eventBroker;
  [WeakEvent]
  public event EventHandler<EventArgs> Event
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
  private event EventHandler<EventArgs> Event_WeakEvent
  {
    add
    {
      weakEventContainerForEvent.AddHandler(value);
    }
    remove
    {
      weakEventContainerForEvent.RemoveHandler(value);
    }
  }
  private void Event_Invoke_WeakEvent(EventHandler<EventArgs> handler, ref (object? sender, EventArgs e) args)
  {
    weakEventContainerForEvent.Invoke((args.sender, args.e));
  }
  public void OnEventField()
  {
    this._eventField.Invoke(this, EventArgs.Empty);
  }
  public void OnEvent()
  {
    foreach (var @delegate in this._delegates)
    {
      @delegate.Invoke(this, EventArgs.Empty);
    }
  }
  private WeakEventContainer<EventHandler<EventArgs>, (object? , EventArgs), WeakEventInvokerForEventHandler<EventArgs>> weakEventContainerForEvent;
  private WeakEventContainer<EventHandler<EventArgs>, (object? , EventArgs), WeakEventInvokerForEventHandler<EventArgs>> weakEventContainerForEventField;
}