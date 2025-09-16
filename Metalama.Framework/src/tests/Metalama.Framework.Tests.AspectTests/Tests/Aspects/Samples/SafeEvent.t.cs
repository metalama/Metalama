internal class TargetCode
{
  private static readonly Func<ActionEventBroker<EventHandler, (object? , EventArgs)>, EventHandler> EventHandlerCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly Action<EventHandler, object, (object? , EventArgs)> EventFieldInvokeDelegate_0 = static (handler, me, args) => ((TargetCode)me).EventField_Raise_SafeEvent(handler, args);
  private static readonly Action<EventHandler, object> EventFieldAddDelegate_0 = static (handler, me) => ((TargetCode)me).EventField_SafeEvent += handler;
  private static readonly Action<EventHandler, object> EventFieldRemoveDelegate_0 = static (handler, me) => ((TargetCode)me).EventField_SafeEvent -= handler;
  private static readonly ActionEventBrokerDelegateSet<EventHandler, (object? , EventArgs)> EventFieldDelegateSet_0 = new ActionEventBrokerDelegateSet<EventHandler, (object? , EventArgs)>(EventFieldInvokeDelegate_0, EventHandlerCastDelegate_0, EventFieldAddDelegate_0, EventFieldRemoveDelegate_0);
  private static readonly Action<EventHandler, object, (object? , EventArgs)> EventInvokeDelegate_0 = static (handler, me, args) => ((TargetCode)me).Event_Raise_SafeEvent(handler, args);
  private static readonly Action<EventHandler, object> EventAddDelegate_0 = static (handler, me) => ((TargetCode)me).Event_SafeEvent += handler;
  private static readonly Action<EventHandler, object> EventRemoveDelegate_0 = static (handler, me) => ((TargetCode)me).Event_SafeEvent -= handler;
  private static readonly ActionEventBrokerDelegateSet<EventHandler, (object? , EventArgs)> EventDelegateSet_0 = new ActionEventBrokerDelegateSet<EventHandler, (object? , EventArgs)>(EventInvokeDelegate_0, EventHandlerCastDelegate_0, EventAddDelegate_0, EventRemoveDelegate_0);
  private List<EventHandler> _delegates = new List<EventHandler>();
  private event EventHandler _eventField = default !;
  private volatile ActionEventBroker<EventHandler, (object? , EventArgs)>? _eventFieldBroker;
  [SafeEvent]
  public event EventHandler EventField
  {
    add
    {
      ActionEventBroker<EventHandler, (object? , EventArgs)>.EnsureInitialized(ref this._eventFieldBroker, this, EventFieldDelegateSet_0);
      this._eventFieldBroker.AddHandler(value);
    }
    remove
    {
      this._eventFieldBroker.RemoveHandler(value);
    }
  }
  private event EventHandler EventField_SafeEvent
  {
    add
    {
      this._eventField += value;
    }
    remove
    {
      this._eventField -= value;
    }
  }
  private void EventField_Raise_SafeEvent(EventHandler handler, (object? sender, EventArgs e) args)
  {
    try
    {
      handler.Invoke(args.sender, args.e);
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      _eventField -= handler;
      throw;
    }
  }
  private volatile ActionEventBroker<EventHandler, (object? , EventArgs)>? _eventBroker;
  [SafeEvent]
  public event EventHandler Event
  {
    add
    {
      ActionEventBroker<EventHandler, (object? , EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventDelegateSet_0);
      this._eventBroker.AddHandler(value);
    }
    remove
    {
      this._eventBroker.RemoveHandler(value);
    }
  }
  private event EventHandler Event_Source
  {
    add
    {
      this._delegates.Add(value);
    }
    remove
    {
      this._delegates.Remove(value);
    }
  }
  private event EventHandler Event_SafeEvent
  {
    add
    {
      this.Event_Source += value;
    }
    remove
    {
      this.Event_Source -= value;
    }
  }
  private void Event_Raise_SafeEvent(EventHandler handler, (object? sender, EventArgs e) args)
  {
    try
    {
      handler.Invoke(args.sender, args.e);
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      Event_Source -= handler;
      throw;
    }
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
}