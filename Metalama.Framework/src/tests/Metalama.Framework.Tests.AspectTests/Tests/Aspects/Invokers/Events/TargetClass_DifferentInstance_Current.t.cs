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
    {
      this._instance!.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance_Current.TargetClass.StaticTarget;
    }
    remove
    {
      this._instance!.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance_Current.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}