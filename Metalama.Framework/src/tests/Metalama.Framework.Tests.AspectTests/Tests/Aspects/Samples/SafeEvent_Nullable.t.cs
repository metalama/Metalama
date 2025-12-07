internal class TargetCode
{
  private static readonly DelegateEventAdapter<EventHandler, (object? , EventArgs), TargetCode> NullableEventFieldAdapter_0 = new(static (EventHandler handler, ref (object? sender, EventArgs e) args, TargetCode me) => me.NullableEventField_Invoke_SafeEvent(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.NullableEventField_SafeEvent += handler, static (handler, me) => me.NullableEventField_SafeEvent -= handler);
  private static readonly DelegateEventAdapter<EventHandler, (object? , EventArgs), TargetCode> NullableEventAdapter_0 = new(static (EventHandler handler, ref (object? sender, EventArgs e) args, TargetCode me) => me.NullableEvent_Invoke_SafeEvent(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.NullableEvent_SafeEvent += handler, static (handler, me) => me.NullableEvent_SafeEvent -= handler);
  private List<EventHandler> _delegates = new List<EventHandler>();
  private event EventHandler? _nullableEventField;
  private volatile EventBroker<EventHandler, (object? , EventArgs), TargetCode>? _nullableEventFieldBroker;
  // Field-like event with nullable delegate type.
  [SafeEvent]
  public event EventHandler? NullableEventField
  {
    add
    {
      EventBroker.EnsureInitialized(ref this._nullableEventFieldBroker, NullableEventFieldAdapter_0, this);
      this._nullableEventFieldBroker.AddHandler(value);
    }
    remove
    {
      this._nullableEventFieldBroker?.RemoveHandler(value);
    }
  }
  private event EventHandler? NullableEventField_SafeEvent
  {
    add
    {
      this._nullableEventField += value;
    }
    remove
    {
      this._nullableEventField -= value;
    }
  }
  private void NullableEventField_Invoke_SafeEvent(EventHandler? handler, ref (object? sender, EventArgs e) args)
  {
    try
    {
      handler.Invoke(args.sender, args.e);
      return;
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      _nullableEventField -= handler;
      throw;
    }
  }
  private volatile EventBroker<EventHandler, (object? , EventArgs), TargetCode>? _nullableEventBroker;
  // Explicitly-implemented event with nullable delegate type.
  [SafeEvent]
  public event EventHandler? NullableEvent
  {
    add
    {
      EventBroker.EnsureInitialized(ref this._nullableEventBroker, NullableEventAdapter_0, this);
      this._nullableEventBroker.AddHandler(value);
    }
    remove
    {
      this._nullableEventBroker?.RemoveHandler(value);
    }
  }
  private event EventHandler? NullableEvent_Source
  {
    add
    {
      if (value != null)
      {
        this._delegates.Add(value);
      }
    }
    remove
    {
      if (value != null)
      {
        this._delegates.Remove(value);
      }
    }
  }
  private event EventHandler? NullableEvent_SafeEvent
  {
    add
    {
      this.NullableEvent_Source += value;
    }
    remove
    {
      this.NullableEvent_Source -= value;
    }
  }
  private void NullableEvent_Invoke_SafeEvent(EventHandler? handler, ref (object? sender, EventArgs e) args)
  {
    try
    {
      handler.Invoke(args.sender, args.e);
      return;
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      NullableEvent_Source -= handler;
      throw;
    }
  }
  public void OnNullableEventField()
  {
    this._nullableEventField?.Invoke(this, EventArgs.Empty);
  }
  public void OnNullableEvent()
  {
    foreach (var @delegate in this._delegates)
    {
      @delegate.Invoke(this, EventArgs.Empty);
    }
  }
}
