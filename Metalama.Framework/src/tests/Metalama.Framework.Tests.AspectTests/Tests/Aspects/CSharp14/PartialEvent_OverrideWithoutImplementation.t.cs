internal partial class C
{
  private static readonly global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)> E2BrokerCallbacks_0 = new global::Metalama.Framework.RunTime.ActionEventBrokerCallbacks<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>(static (handler, me, args) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialEvent_OverrideWithoutImplementation.C)me).E2_Invoke_TheAspect(handler, args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialEvent_OverrideWithoutImplementation.C)me).E2_TheAspect += handler, static (handler, me) => ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialEvent_OverrideWithoutImplementation.C)me).E2_TheAspect -= handler);
  [TheAspect]
  public partial event EventHandler E1;
  public partial event EventHandler E1
  {
    add
    {
      global::System.Console.WriteLine("Add");
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
    }
  }
  [TheAspect(OverrideInvoke = true)]
  public partial event EventHandler E2;
  private event EventHandler _e2 = default !;
  private volatile global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>? _e2Broker;
  public partial event EventHandler E2
  {
    add
    {
      global::Metalama.Framework.RunTime.ActionEventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs)>.EnsureInitialized(ref this._e2Broker, this, E2BrokerCallbacks_0);
      this._e2Broker.AddHandler(value);
    }
    remove
    {
      this._e2Broker?.RemoveHandler(value);
    }
  }
  private event global::System.EventHandler E2_TheAspect
  {
    add
    {
      global::System.Console.WriteLine("Add");
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
    }
  }
  private void E2_Invoke_TheAspect(global::System.EventHandler handler, (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
    return;
  }
}