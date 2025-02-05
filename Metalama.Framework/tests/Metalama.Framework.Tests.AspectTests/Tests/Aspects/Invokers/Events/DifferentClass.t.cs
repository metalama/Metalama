public class TargetClass
{
  private DifferentClass? instance;
  [InvokerAspect]
  public event EventHandler Invoker
  {
    add
    { // Invoke instance.Event
      this.instance.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.DifferentClass.TargetClass.StaticTarget;
      // Invoke instance.Event
      this.instance.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.DifferentClass.TargetClass.StaticTarget;
    }
    remove
    { // Invoke instance.Event
      this.instance.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.DifferentClass.TargetClass.StaticTarget;
      // Invoke instance.Event
      this.instance.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.DifferentClass.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}