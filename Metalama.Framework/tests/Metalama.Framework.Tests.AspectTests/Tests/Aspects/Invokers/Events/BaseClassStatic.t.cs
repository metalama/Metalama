public class TargetClass : BaseClass
{
  [InvokerAspect]
  public event EventHandler Invoker
  {
    add
    { // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.BaseClass.Event += global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.TargetClass.StaticTarget;
    }
    remove
    { // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.TargetClass.StaticTarget;
      // Invoke BaseClass.Event
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.BaseClass.Event -= global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassStatic.TargetClass.StaticTarget;
    }
  }
  public static void StaticTarget(object? sender, EventArgs args)
  {
  }
}