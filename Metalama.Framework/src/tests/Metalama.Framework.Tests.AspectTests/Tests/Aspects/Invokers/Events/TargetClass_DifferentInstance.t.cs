public class TargetClass
{
  public event EventHandler Event
  {
    add
    {
    }
    remove
    {
    }
  }
  private TargetClass? _instance;
  [InvokerAspect]
  public event EventHandler Invoker
  {
    add
    { // Invoke _instance.Event
      this._instance!.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance.TargetClass.StaticTarget;
      // Invoke _instance.Event
      this._instance!.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance.TargetClass.StaticTarget;
    }
    remove
    { // Invoke _instance.Event
      this._instance!.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance.TargetClass.StaticTarget;
      // Invoke _instance.Event
      this._instance!.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}