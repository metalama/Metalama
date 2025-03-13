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
    { // Invoke instance.Event
      this.instance!.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance.TargetClass.StaticTarget;
      // Invoke instance.Event
      this.instance!.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance.TargetClass.StaticTarget;
    }
    remove
    { // Invoke instance.Event
      this.instance!.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance.TargetClass.StaticTarget;
      // Invoke instance.Event
      this.instance!.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_DifferentInstance.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}