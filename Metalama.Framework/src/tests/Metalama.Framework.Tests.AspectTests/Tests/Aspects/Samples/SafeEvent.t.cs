internal class TargetCode
{
  private static readonly ActionEventBrokerCallbacks<EventHandler, (object? , EventArgs)> EventFieldBrokerCallbacks_0 = new ActionEventBrokerCallbacks<EventHandler, (object? , EventArgs)>(static (handler, me, args) => ((TargetCode)me).EventField_Invoke_SafeEvent(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((TargetCode)me).EventField_SafeEvent += handler, static (handler, me) => ((TargetCode)me).EventField_SafeEvent -= handler);
  private static readonly ActionEventBrokerCallbacks<EventHandler, (object? , EventArgs)> EventBrokerCallbacks_0 = new ActionEventBrokerCallbacks<EventHandler, (object? , EventArgs)>(static (handler, me, args) => ((TargetCode)me).Event_Invoke_SafeEvent(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((TargetCode)me).Event_SafeEvent += handler, static (handler, me) => ((TargetCode)me).Event_SafeEvent -= handler);
  private List<EventHandler> _delegates = new List<EventHandler>();
  private event EventHandler _eventField = default !;
  private volatile ActionEventBroker<EventHandler, (object? , EventArgs)>? _eventFieldBroker;
  [SafeEvent]
  public event EventHandler EventField
  {
    add
    {
      ActionEventBroker<EventHandler, (object? , EventArgs)>.EnsureInitialized(ref this._eventFieldBroker, this, EventFieldBrokerCallbacks_0);
      this._eventFieldBroker.AddHandler(value);
    }
    remove
    {
      this._eventFieldBroker?.RemoveHandler(value);
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
  private void EventField_Invoke_SafeEvent(EventHandler handler, (object? sender, EventArgs e) args)
  {
    try
    {
      handler.Invoke(args.sender, args.e);
      return;
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
      ActionEventBroker<EventHandler, (object? , EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventBrokerCallbacks_0);
      this._eventBroker.AddHandler(value);
    }
    remove
    {
      this._eventBroker?.RemoveHandler(value);
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
  private void Event_Invoke_SafeEvent(EventHandler handler, (object? sender, EventArgs e) args)
  {
    try
    {
      handler.Invoke(args.sender, args.e);
      return;
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