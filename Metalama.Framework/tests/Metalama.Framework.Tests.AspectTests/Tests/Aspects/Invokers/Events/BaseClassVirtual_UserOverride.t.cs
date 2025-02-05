public class TargetClass : BaseClass
{
  public override event EventHandler Event
  {
    add
    {
      this.Event_Source += value;
    }
    remove
    {
      this.Event_Source -= value;
    }
  }
  private event EventHandler Event_Source
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
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_UserOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_UserOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_UserOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_UserOverride.TargetClass.StaticTarget;
    }
    remove
    { // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_UserOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_UserOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_UserOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_UserOverride.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}