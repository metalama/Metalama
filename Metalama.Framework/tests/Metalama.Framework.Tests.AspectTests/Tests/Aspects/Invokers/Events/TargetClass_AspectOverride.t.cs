public class TargetClass
{
  [OverrideAspect]
  public event EventHandler Event
  {
    add
    { // Invoke this.Event_Source
      this.Event_Source += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source += value;
    }
    remove
    { // Invoke this.Event_Source
      this.Event_Source -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
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
  [InvokerBeforeAspect]
  public event EventHandler InvokerBefore
  {
    add
    { // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
    // Invoke this.Event_Source
    }
    remove
    { // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event_Source
      this.Event_Source -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
    // Invoke this.Event_Source
    }
  }
  [InvokerAfterAspect]
  public event EventHandler InvokerAfter
  {
    add
    { // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
    // Invoke this.Event
    }
    remove
    { // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
      // Invoke this.Event
      this.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.TargetClass_AspectOverride.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}