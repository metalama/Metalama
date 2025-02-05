public class TargetClass : BaseClass
{
  public new event EventHandler Event
  {
    add
    {
    }
    remove
    {
    }
  }
  [InvokerAspect]
  public event EventHandler Invoker
  {
    add
    { // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_Hidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_Hidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_Hidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_Hidden.TargetClass.StaticTarget;
    }
    remove
    { // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_Hidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_Hidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_Hidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_Hidden.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}