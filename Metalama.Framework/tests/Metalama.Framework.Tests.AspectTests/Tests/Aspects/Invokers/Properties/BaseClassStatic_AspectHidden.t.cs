[IntroductionAspect]
public class TargetClass : BaseClass
{
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    {
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property;
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property;
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property;
      return 0;
    }
    set
    { // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property = 42;
    }
  }
  [InvokerAfterAspect]
  public int InvokerAfter
  {
    get
    {
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property;
      return 0;
    }
    set
    { // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property = 42;
    }
  }
  public static new global::System.Int32 Property
  {
    get
    {
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property;
      // Invoke BaseClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property;
      // Invoke BaseClass.Property
      return global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property;
    }
    set
    {
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.TargetClass.Property = 42;
      // Invoke BaseClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_AspectHidden.BaseClass.Property = value;
    }
  }
}