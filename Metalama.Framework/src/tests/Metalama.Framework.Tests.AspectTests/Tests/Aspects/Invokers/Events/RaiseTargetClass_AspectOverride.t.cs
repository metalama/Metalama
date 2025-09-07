public class TargetClass
{
  private static readonly global::System.Func<global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>, global::System.EventHandler> EventHandlerCastDelegate_0 = static b => (sender, e) => b.Invoke((sender, e));
  private static readonly global::System.Action<global::System.EventHandler, global::System.Object, (global::System.Object? , global::System.EventArgs)> EventInvokeDelegate_0 = static (handler, me, args) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.RaiseTargetClass_AspectOverride.TargetClass)me).Event_OverrideAspect_Invoke(handler, args);
  private event EventHandler _event = default !;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  [OverrideAspect]
  public event EventHandler Event
  {
    add
    {
      if (this._eventBroker == null)
      {
        global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.InitializeField(ref this._eventBroker, this, EventInvokeDelegate_0, EventHandlerCastDelegate_0);
      }
      if (this._eventBroker.AddHandler(value))
      {
        this.Event_OverrideAspect += this._eventBroker.InvocationDelegate;
      }
    }
    remove
    {
      if (this._eventBroker != null && this._eventBroker.RemoveHandler(value))
      {
        this.Event_OverrideAspect -= this._eventBroker.InvocationDelegate;
      }
    }
  }
  private event global::System.EventHandler Event_OverrideAspect
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
  private void Event_OverrideAspect_Invoke(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    handler.Invoke(args.sender, args.e)?.Invoke(null, global::System.EventArgs.Empty);
    handler.Invoke(args.sender, args.e)?.Invoke(null, global::System.EventArgs.Empty);
    handler.Invoke(args.sender, args.e)?.Invoke(null, global::System.EventArgs.Empty);
    handler.Invoke(args.sender, args.e)?.Invoke(null, global::System.EventArgs.Empty);
    handler.Invoke(args.sender, args.e);
  }
  [InvokerAspectBefore]
  public void BeforeOverride()
  {
    // Invoke this.Event
    this._event?.Invoke(null, global::System.EventArgs.Empty);
    // Invoke this.Event
    this._event?.Invoke(null, global::System.EventArgs.Empty);
    // Invoke this.Event
    this._event?.Invoke(null, global::System.EventArgs.Empty);
    // Invoke this.Event
    this._event?.Invoke(null, global::System.EventArgs.Empty);
  }
  [InvokerAspectAfter]
  public void AfterOverride()
  {
    // Invoke this.Event
    this._event?.Invoke(null, global::System.EventArgs.Empty);
    // Invoke this.Event
    this._event?.Invoke(null, global::System.EventArgs.Empty);
    // Invoke this.Event
    this._event?.Invoke(null, global::System.EventArgs.Empty);
    // Invoke this.Event
    this._event?.Invoke(null, global::System.EventArgs.Empty);
  }
}