public class TargetClass : BaseClass
{
  [OverrideAspect]
  public new static int Property
  {
    get
    {
      // Invoke TargetClass.Property_Source
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property_Source;
      // Invoke TargetClass.Property_Source
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property_Source;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property;
      // Invoke TargetClass.Property_Source
      return global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property_Source;
    }
    set
    { // Invoke TargetClass.Property_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property_Source = 42;
      // Invoke TargetClass.Property_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property_Source = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property = 42;
      // Invoke TargetClass.Property_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.TargetClass.Property_Source = value;
    }
  }
  private static int Property_Source
  {
    get
    {
      return 0;
    }
    set
    {
    }
  }
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    {
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property;
      // Invoke TargetClass.Property_Source
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property;
      // Invoke TargetClass.Property_Source
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property;
      return 0;
    }
    set
    { // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property = 42;
      // Invoke TargetClass.Property_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property = 42;
      // Invoke TargetClass.Property_Source
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property = 42;
    }
  }
  [InvokerAfterAspect]
  public int InvokerAfter
  {
    get
    {
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property;
      // Invoke TargetClass.Property
      _ = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property;
      // Invoke TargetClass.Property
      return 0;
    }
    set
    { // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property = 42;
      // Invoke TargetClass.Property
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride.BaseClass.Property = 42;
    }
  }
}