internal class TargetCode
{
  private static readonly Func<ActionEventBroker<EventHandler, (object? , EventArgs)>, EventHandler> EventHandlerCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly Action<EventHandler, object, (object? , EventArgs)> EventFieldInvokeDelegate_0 = static (handler, me, args) => ((TargetCode)me).EventField_Raise_SafeEvent( handler, args);
  private static readonly Action<EventHandler, object, (object? , EventArgs)> EventInvokeDelegate_0 = static (handler, me, args) => ((TargetCode)me).Event_Raise_SafeEvent( handler, args);
  private List<EventHandler> _delegates = new List<EventHandler>();
  private event EventHandler _eventField = default !;
  private volatile ActionEventBroker<EventHandler, (object? , EventArgs)>? _eventFieldBroker;
  [SafeEvent]
  public event EventHandler EventField
  {
    add
    {
      ActionEventBroker<EventHandler, (object? , EventArgs)>.EnsureInitialized(ref this._eventFieldBroker, this, EventFieldInvokeDelegate_0, EventHandlerCastDelegate_0);
      if (this._eventFieldBroker.AddHandler(value))
      {
        this.EventField_SafeEvent += this._eventFieldBroker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._eventFieldBroker != null && this._eventFieldBroker.RemoveHandler(value))
      {
        this.EventField_SafeEvent -= this._eventFieldBroker.InvocationDelegate;
      }
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
  private void EventField_Raise_SafeEvent( EventHandler handler, (object? sender, EventArgs e) args)
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
      ActionEventBroker<EventHandler, (object? , EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventInvokeDelegate_0, EventHandlerCastDelegate_0);
      if (this._eventBroker.AddHandler(value))
      {
        this.Event_SafeEvent += this._eventBroker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._eventBroker != null && this._eventBroker.RemoveHandler(value))
      {
        this.Event_SafeEvent -= this._eventBroker.InvocationDelegate;
      }
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
  private void Event_Raise_SafeEvent( EventHandler handler, (object? sender, EventArgs e) args)
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