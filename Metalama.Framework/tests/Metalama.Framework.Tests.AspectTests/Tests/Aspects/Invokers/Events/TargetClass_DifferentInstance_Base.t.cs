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
  private TargetClass? instance;
  [InvokerAspect]
  public event EventHandler Invoker
  {
    add
    {
      this.instance!.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance_Base.TargetClass.StaticTarget;
    }
    remove
    {
      this.instance!.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance_Base.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}