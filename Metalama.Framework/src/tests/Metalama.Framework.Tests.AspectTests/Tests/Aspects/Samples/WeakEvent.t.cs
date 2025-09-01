internal class TargetCode
{
  private static readonly Func<ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>, EventHandler<EventArgs>> EventHandlerCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly Action<EventHandler<EventArgs>, object, (object? , EventArgs)> EventFieldInvokeDelegate_0 = static (handler, me, args) => ((TargetCode)me).EventField_WeakEvent_Invoke(handler, args);
  private static readonly Action<EventHandler<EventArgs>, object, (object? , EventArgs)> EventInvokeDelegate_0 = static (handler, me, args) => ((TargetCode)me).Event_WeakEvent_Invoke(handler, args);
  private List<EventHandler<EventArgs>> _delegates = new List<EventHandler<EventArgs>>();
  private event EventHandler<EventArgs> _eventField = default !;
  private volatile ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>? _eventFieldBroker;
  [WeakEvent]
  public event EventHandler<EventArgs> EventField
  {
    add
    {
      if (this._eventFieldBroker == null)
      {
        ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>.InitializeField(ref this._eventFieldBroker, this, EventFieldInvokeDelegate_0, EventHandlerCastDelegate_0);
      }
      if (this._eventFieldBroker.AddHandler(value))
      {
        this.EventField_WeakEvent += this._eventFieldBroker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._eventFieldBroker != null && this._eventFieldBroker.RemoveHandler(value))
      {
        this.EventField_WeakEvent -= this._eventFieldBroker.InvocationDelegate;
      }
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
  private void EventField_WeakEvent_Invoke(EventHandler<EventArgs> handler, (object? sender, EventArgs e) args)
  {
    weakEventContainerForEventField.Invoke((args.sender, args.e));
  }
  private volatile ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>? _eventBroker;
  [WeakEvent]
  public event EventHandler<EventArgs> Event
  {
    add
    {
      if (this._eventBroker == null)
      {
        ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>.InitializeField(ref this._eventBroker, this, EventInvokeDelegate_0, EventHandlerCastDelegate_0);
      }
      if (this._eventBroker.AddHandler(value))
      {
        this.Event_WeakEvent += this._eventBroker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._eventBroker != null && this._eventBroker.RemoveHandler(value))
      {
        this.Event_WeakEvent -= this._eventBroker.InvocationDelegate;
      }
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
  private void Event_WeakEvent_Invoke(EventHandler<EventArgs> handler, (object? sender, EventArgs e) args)
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