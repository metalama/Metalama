public class TargetClass
{
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)> EventDelegateSet_0 = new global::Metalama.Framework.RunTime.ActionEventBrokerDelegateSet<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>(static (handler, me, args) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.RaiseTargetClass_AspectOverride.TargetClass)me).Event_Raise_OverrideAspect(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.RaiseTargetClass_AspectOverride.TargetClass)me).Event_OverrideAspect += handler, static (handler, me) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.RaiseTargetClass_AspectOverride.TargetClass)me).Event_OverrideAspect -= handler);
  private event EventHandler _event = default !;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _eventBroker;
  [OverrideAspect]
  public event EventHandler Event
  {
    add
    {
      global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._eventBroker, this, EventDelegateSet_0);
      this._eventBroker.AddHandler(value);
    }
    remove
    {
      this._eventBroker.RemoveHandler(value);
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
  private void Event_Raise_OverrideAspect(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    // Invoke handler
    handler.Invoke((global::System.Object? )null, global::System.EventArgs.Empty);
    // Invoke handler
    handler.Invoke((global::System.Object? )null, global::System.EventArgs.Empty);
    // TODO: Invoke this._eventBroker.Invoke
    // TODO: Invoke this._eventBroker.Invoke
    // Invoke handler
    handler.Invoke(args.sender, args.e);
  }
  [InvokerAspectBefore]
  public void BeforeOverride()
  {
    // TODO: Invoke this._event
    // TODO: Invoke this._event
    // TODO: Invoke this._event
    // Invoke this._eventBroker.Invoke
    this._eventBroker.Invoke(((global::System.Object? )null, global::System.EventArgs.Empty));
  }
  [InvokerAspectAfter]
  public void AfterOverride()
  {
    // Invoke this._eventBroker.Invoke
    this._eventBroker.Invoke(((global::System.Object? )null, global::System.EventArgs.Empty));
    // Invoke this._eventBroker.Invoke
    this._eventBroker.Invoke(((global::System.Object? )null, global::System.EventArgs.Empty));
    // Invoke this._eventBroker.Invoke
    this._eventBroker.Invoke(((global::System.Object? )null, global::System.EventArgs.Empty));
    // Invoke this._eventBroker.Invoke
    this._eventBroker.Invoke(((global::System.Object? )null, global::System.EventArgs.Empty));
  }
}