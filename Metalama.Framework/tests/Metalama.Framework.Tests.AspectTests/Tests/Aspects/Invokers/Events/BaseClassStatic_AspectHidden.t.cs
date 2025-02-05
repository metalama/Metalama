[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public event EventHandler InvokerBefore
  {
    add
    { // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
    }
    remove
    { // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
    }
  }
  [InvokerAfterAspect]
  public event EventHandler InvokerAfter
  {
    add
    { // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
    }
    remove
    { // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
  public static new event global::System.EventHandler Event
  {
    add
    {
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event += value;
    }
    remove
    {
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_AspectHidden.BaseClass.Event -= value;
    }
  }
}