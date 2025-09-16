internal class TargetCode
{
  private static readonly Func<ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>, EventHandler<EventArgs>> EventHandlerCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly Action<EventHandler<EventArgs>, object, (object? , EventArgs)> EventFieldInvokeDelegate_0 = static (handler, me, args) => ((TargetCode)me).EventField_Raise_WeakEvent(handler, args);
  private static readonly Action<EventHandler<EventArgs>, object> EventFieldAddDelegate_0 = static (handler, me) => ((TargetCode)me).EventField_WeakEvent += handler;
  private static readonly Action<EventHandler<EventArgs>, object> EventFieldRemoveDelegate_0 = static (handler, me) => ((TargetCode)me).EventField_WeakEvent -= handler;
  private static readonly ActionEventBrokerDelegateSet<EventHandler<EventArgs>, (object? , EventArgs)> EventFieldDelegateSet_0 = new ActionEventBrokerDelegateSet<EventHandler<EventArgs>, (object? , EventArgs)>(EventFieldInvokeDelegate_0, EventHandlerCastDelegate_0, EventFieldAddDelegate_0, EventFieldRemoveDelegate_0);
  private static readonly Action<EventHandler<EventArgs>, object, (object? , EventArgs)> EventInvokeDelegate_0 = static (handler, me, args) => ((TargetCode)me).Event_Raise_WeakEvent(handler, args);
  private static readonly Action<EventHandler<EventArgs>, object> EventAddDelegate_0 = static (handler, me) => ((TargetCode)me).Event_WeakEvent += handler;
  private static readonly Action<EventHandler<EventArgs>, object> EventRemoveDelegate_0 = static (handler, me) => ((TargetCode)me).Event_WeakEvent -= handler;
  private static readonly ActionEventBrokerDelegateSet<EventHandler<EventArgs>, (object? , EventArgs)> EventDelegateSet_0 = new ActionEventBrokerDelegateSet<EventHandler<EventArgs>, (object? , EventArgs)>(EventInvokeDelegate_0, EventHandlerCastDelegate_0, EventAddDelegate_0, EventRemoveDelegate_0);
  private List<EventHandler<EventArgs>> _delegates = new List<EventHandler<EventArgs>>();
  private event EventHandler<EventArgs> _eventField = default !;
  private volatile ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>? _eventFieldBroker;
  [WeakEvent]
  public event EventHandler<EventArgs> EventField
  {
    add
    {
      ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>.EnsureInitialized(ref this._eventFieldBroker, this, EventFieldDelegateSet_0);
      this._eventFieldBroker.AddHandler(value);
    }
    remove
    {
      this._eventFieldBroker.RemoveHandler(value);
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
  private void EventField_Raise_WeakEvent(EventHandler<EventArgs> handler, (object? sender, EventArgs e) args)
  {
    weakEventContainerForEventField.Invoke((args.sender, args.e));
  }
  private volatile ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>? _eventBroker;
  [WeakEvent]
  public event EventHandler<EventArgs> Event
  {
    add
    {
      ActionEventBroker<EventHandler<EventArgs>, (object? , EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventDelegateSet_0);
      this._eventBroker.AddHandler(value);
    }
    remove
    {
      this._eventBroker.RemoveHandler(value);
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
  private void Event_Raise_WeakEvent(EventHandler<EventArgs> handler, (object? sender, EventArgs e) args)
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