[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public event EventHandler InvokerBefore
  {
    add
    { // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke base.Event
      base.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke base.Event
      base.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
    }
    remove
    { // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke base.Event
      base.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke base.Event
      base.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
    }
  }
  [InvokerAfterAspect]
  public event EventHandler InvokerAfter
  {
    add
    { // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
    }
    remove
    { // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
  public new event global::System.EventHandler Event
  {
    add
    {
      // Invoke base.Event
      base.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke base.Event
      base.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke base.Event
      base.Event += value;
    }
    remove
    {
      // Invoke base.Event
      base.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke base.Event
      base.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden.TargetClass.StaticTarget;
      // Invoke base.Event
      base.Event -= value;
    }
  }
}