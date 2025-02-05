public class TargetClass : BaseClass
{
  [OverrideAspect]
  public new static event EventHandler Event
  {
    add
    { // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event_Source += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event_Source += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event_Source += value;
    }
    remove
    { // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event_Source -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event_Source -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Event_Source -= value;
    }
  }
  private static event EventHandler Event_Source
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
    { // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
    }
    remove
    { // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
    }
  }
  [InvokerAfterAspect]
  public event EventHandler InvokerAfter
  {
    add
    { // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
    // Invoke TargetClass.Event
    }
    remove
    { // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
      // Invoke TargetClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic_UserHidden_AspectOverride.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}