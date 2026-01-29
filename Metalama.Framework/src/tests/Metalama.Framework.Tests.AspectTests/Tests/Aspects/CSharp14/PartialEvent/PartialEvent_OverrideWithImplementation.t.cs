internal partial class C
{
  private static readonly global::Metalama.Framework.RunTime.Events.DelegateEventAdapter<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialEvent_OverrideWithImplementation.C> E2Adapter_0 = new(static (handler, ref args, me) => me.E2_Invoke_TheAspect(handler, ref args), static b => (sender, e) => b.Invoke((sender, e)), static (handler, me) => me.E2_TheAspect += handler, static (handler, me) => me.E2_TheAspect -= handler);
  public partial event EventHandler E1;
  [TheAspect]
  public partial event EventHandler E1
  {
    add
    {
      global::System.Console.WriteLine("Add");
      Console.WriteLine("Original Add");
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
      Console.WriteLine("Original Remove");
    }
  }
  public partial event EventHandler E2;
  private volatile global::Metalama.Framework.RunTime.Events.EventBroker<global::System.EventHandler, (global::System.Object? , global::System.EventArgs), global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialEvent_OverrideWithImplementation.C>? _e2Broker;
  [TheAspect(OverrideInvoke = true)]
  public partial event EventHandler E2
  {
    add
    {
      global::Metalama.Framework.RunTime.Events.EventBroker.EnsureInitialized(ref this._e2Broker, E2Adapter_0, this);
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
      Console.WriteLine("Original Add");
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
      Console.WriteLine("Original Remove");
    }
  }
  private void E2_Invoke_TheAspect(global::System.EventHandler handler, ref (global::System.Object? sender, global::System.EventArgs e) args)
  {
    global::System.Console.WriteLine("Invoke");
    handler.Invoke(args.sender, args.e);
    return;
  }
}